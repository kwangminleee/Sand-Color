using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 물리 모래 입자가 바닥에 닿으면 고정 크기 격자의 모래로 변환하고,
/// 격자 안에서 아래 또는 대각선 방향으로 흐르며 쌓이도록 관리합니다.
/// </summary>
public sealed class SandPileController : MonoBehaviour
{
    private const int TargetGridWidth = 320;
    private const int MaxSurfaceDetails = 128;
    private const int MaxTextureSize = 1024;
    private const float SandTextureVariation = 0.025f;

    private static SandPileController _instance;

    private readonly List<EdgeCollider2D> _surfaceColliders = new List<EdgeCollider2D>();
    private readonly List<SpriteRenderer> _surfaceDetails = new List<SpriteRenderer>();
    private Texture2D _texture;
    private Texture2D _detailTexture;
    private Sprite _detailSprite;
    private Color32[] _cells;
    private Color32[] _displayCells;
    private Color32[] _moldCells;
    private Color32[] _guideCells;
    private bool[] _templateCells;
    private int[] _solidFloor;
    private SpriteRenderer _renderer;
    private EdgeCollider2D _boundsCollider;
    private RectTransform _uiTarget;
    private RawImage _uiRenderer;
    private Rect? _configuredBounds;
    private int _width;
    private int _height;
    private float _pixelsPerUnit;
    private int _simulationStepsPerFrame = 3;
    private bool _preferLeft = true;
    private bool _cellsDirty;
    private bool _surfaceDirty;
    private Transform _surfaceDetailContainer;

    public static SandPileController Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject controller = new GameObject("Sand Pile Controller");
                _instance = controller.AddComponent<SandPileController>();
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        BuildGrid();
    }

    private void Update()
    {
        for (int i = 0; i < _simulationStepsPerFrame; i++)
        {
            SimulateSand();
        }

        if (_cellsDirty)
        {
            UpdateDisplayTexture();
            _texture.SetPixels32(_displayCells);
            _texture.Apply(false, false);
            _cellsDirty = false;
        }
    }

    private void FixedUpdate()
    {
        if (_surfaceDirty)
        {
            RebuildSurfaceColliders();
            _surfaceDirty = false;
        }
    }

    public void AddSand(Vector3 worldPosition, Color color)
    {
        Vector2Int cell = WorldToCell(worldPosition);
        if (!IsInside(cell.x, cell.y))
        {
            return;
        }

        // 충돌 위치가 이미 차 있으면 바로 위에서 가장 가까운 빈칸을 찾습니다.
        int targetY = cell.y;
        while (targetY < _height && IsOccupied(cell.x, targetY))
        {
            targetY++;
        }

        if (targetY >= _height)
        {
            return;
        }

        Color32 sandColor = color;
        // 완성된 그림에서 선택한 색이 선명하게 보이도록 일정한 미세 모래결만 적용합니다.
        // 밝기 변화가 크면 완성된 그림의 색이 탁해지므로 변화 폭을 작게 유지합니다.
        int grainHash = cell.x * 73856093 ^ targetY * 19349663;
        float grain = ((grainHash & 255) / 255f - 0.5f) * 2f;
        float brightness = 1f + grain * SandTextureVariation;
        sandColor.r = (byte)Mathf.Clamp(Mathf.RoundToInt(sandColor.r * brightness), 0, 255);
        sandColor.g = (byte)Mathf.Clamp(Mathf.RoundToInt(sandColor.g * brightness), 0, 255);
        sandColor.b = (byte)Mathf.Clamp(Mathf.RoundToInt(sandColor.b * brightness), 0, 255);
        sandColor.a = 255;
        SetCell(cell.x, targetY, sandColor);
        _cellsDirty = true;
        _surfaceDirty = true;
    }

    public void AddSandBurst(Vector3 worldPosition, Color color, int amount)
    {
        int burstWidth;
        int burstHeight;
        int capacity;
        do
        {
            burstWidth = Random.Range(3, 6);
            burstHeight = Random.Range(2, 5);
            capacity = burstWidth * burstHeight;
        }
        while (capacity < Mathf.Min(amount, 20));

        int targetAmount = Mathf.Clamp(amount, 1, capacity);
        int usedCells = 0;
        int emitted = 0;
        float cellSize = 1f / _pixelsPerUnit;

        while (emitted < targetAmount)
        {
            int index = Random.Range(0, capacity);
            int bit = 1 << index;
            if ((usedCells & bit) != 0)
            {
                continue;
            }

            usedCells |= bit;
            int localX = index % burstWidth - burstWidth / 2;
            int localY = index / burstWidth;
            Vector3 cellPosition = worldPosition + new Vector3(
                localX * cellSize,
                -localY * cellSize,
                0f
            );
            AddSand(cellPosition, color);
            emitted++;
        }
    }

    public void SetSimulationStepsPerFrame(int steps)
    {
        _simulationStepsPerFrame = Mathf.Clamp(steps, 1, 8);
    }

    public bool ContainsSand(Vector3 worldPosition)
    {
        Vector2Int cell = WorldToCell(worldPosition);
        return IsOccupied(cell.x, cell.y);
    }

    public void ConfigureBounds(RectTransform uiTarget, Rect worldBounds)
    {
        if (worldBounds.width <= 0f || worldBounds.height <= 0f)
        {
            return;
        }

        _uiTarget = uiTarget;
        _configuredBounds = worldBounds;
        BuildGrid();
        BuildBoundsCollider(worldBounds.size);
        BuildUiRenderer();
    }

    public int GridWidth => _width;
    public int GridHeight => _height;

    /// <summary>
    /// Fills the play area with fixed sand while leaving a template-shaped cavity.
    /// Falling sand can only settle inside the cavity, so the surrounding sand acts as a mold.
    /// </summary>
    public void BuildTemplateMold(Color32[] mask, int maskWidth, int maskHeight, Color color)
    {
        if (!LoadTemplateMask(mask, maskWidth, maskHeight))
        {
            return;
        }

        if (_moldCells == null || _moldCells.Length != _width * _height)
        {
            _moldCells = new Color32[_width * _height];
        }

        Color32 baseColor = color;
        baseColor.a = 255;
        System.Array.Clear(_guideCells, 0, _guideCells.Length);

        for (int y = 0; y < _height; y++)
        {
            float normalizedY = (y + 0.5f) / _height;
            for (int x = 0; x < _width; x++)
            {
                float normalizedX = (x + 0.5f) / _width;
                int index = y * _width + x;
                if (IsInsideTemplate(normalizedX, normalizedY))
                {
                    _moldCells[index] = default;
                    continue;
                }

                int hash = x * 73856093 ^ y * 19349663;
                float grain = ((hash & 255) / 255f - 0.5f) * 0.1f;
                _moldCells[index] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(baseColor.r * (1f + grain)), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(baseColor.g * (1f + grain)), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(baseColor.b * (1f + grain)), 0, 255),
                    255
                );
            }
        }

        _cellsDirty = true;
        _surfaceDirty = true;
    }

    /// <summary>
    /// Draws a non-physical template outline that remains visible over the sand.
    /// </summary>
    public void BuildTemplateOutline(
        Color32[] mask,
        int maskWidth,
        int maskHeight,
        Color color,
        int thickness = 2)
    {
        if (!LoadTemplateMask(mask, maskWidth, maskHeight))
        {
            return;
        }

        System.Array.Clear(_moldCells, 0, _moldCells.Length);
        System.Array.Clear(_guideCells, 0, _guideCells.Length);

        Color32 guideColor = color;
        guideColor.a = (byte)Mathf.Clamp(Mathf.RoundToInt(color.a * 255f), 1, 255);
        int radius = Mathf.Clamp(thickness, 1, 6);

        for (int y = 0; y < _height; y++)
        {
            float normalizedY = (y + 0.5f) / _height;
            for (int x = 0; x < _width; x++)
            {
                float normalizedX = (x + 0.5f) / _width;
                if (!IsInsideTemplate(normalizedX, normalizedY))
                {
                    continue;
                }

                bool boundary = false;
                for (int offsetY = -radius; offsetY <= radius && !boundary; offsetY++)
                {
                    for (int offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        float sampleX = (x + offsetX + 0.5f) / _width;
                        float sampleY = (y + offsetY + 0.5f) / _height;
                        if (!IsInsideTemplate(sampleX, sampleY))
                        {
                            boundary = true;
                            break;
                        }
                    }
                }

                if (boundary)
                {
                    _guideCells[y * _width + x] = guideColor;
                }
            }
        }

        _cellsDirty = true;
        _surfaceDirty = true;
    }

    public void ClearTemplate()
    {
        System.Array.Clear(_moldCells, 0, _moldCells.Length);
        System.Array.Clear(_guideCells, 0, _guideCells.Length);
        _cellsDirty = true;
        _surfaceDirty = true;
    }

    public void ClearSand()
    {
        System.Array.Clear(_cells, 0, _cells.Length);
        _cellsDirty = true;
        _surfaceDirty = true;
    }

    public void PaintDebugBlock(
        int minX,
        int maxX,
        int minY,
        int maxY,
        Color32 color)
    {
        int clampedMinX = Mathf.Clamp(minX, 0, _width);
        int clampedMaxX = Mathf.Clamp(maxX, clampedMinX, _width);
        int clampedMinY = Mathf.Clamp(minY, 0, _height);
        int clampedMaxY = Mathf.Clamp(maxY, clampedMinY, _height);
        color.a = 255;

        for (int y = clampedMinY; y < clampedMaxY; y++)
        {
            int row = y * _width;
            for (int x = clampedMinX; x < clampedMaxX; x++)
            {
                _cells[row + x] = color;
            }
        }

        _cellsDirty = true;
        _surfaceDirty = true;
    }

    private bool LoadTemplateMask(Color32[] mask, int maskWidth, int maskHeight)
    {
        if (mask == null || maskWidth <= 0 || maskHeight <= 0 || mask.Length != maskWidth * maskHeight)
        {
            return false;
        }

        for (int y = 0; y < _height; y++)
        {
            int sourceY = Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) / _height * maskHeight), 0, maskHeight - 1);
            for (int x = 0; x < _width; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) / _width * maskWidth), 0, maskWidth - 1);
                Color32 sample = mask[sourceY * maskWidth + sourceX];
                int brightness = sample.r + sample.g + sample.b;
                _templateCells[y * _width + x] = sample.a >= 32 && brightness >= 384;
            }
        }

        return true;
    }

    private bool IsInsideTemplate(float normalizedX, float normalizedY)
    {
        int x = Mathf.FloorToInt(normalizedX * _width);
        int y = Mathf.FloorToInt(normalizedY * _height);
        return IsInside(x, y) && _templateCells[y * _width + x];
    }

    public void RegisterSolidSurface(Collider2D solid, Vector2 contactPoint)
    {
        int left = WorldToCell(new Vector3(solid.bounds.min.x, contactPoint.y)).x;
        int right = WorldToCell(new Vector3(solid.bounds.max.x, contactPoint.y)).x;
        int floorY = WorldToCell(contactPoint).y - 1;

        left = Mathf.Clamp(left, 0, _width - 1);
        right = Mathf.Clamp(right, 0, _width - 1);
        floorY = Mathf.Clamp(floorY, -1, _height - 1);

        for (int x = left; x <= right; x++)
        {
            _solidFloor[x] = Mathf.Max(_solidFloor[x], floorY);
        }
    }

    private void SimulateSand()
    {
        bool moved = false;
        bool preferLeftThisStep = _preferLeft;
        _preferLeft = !_preferLeft;

        // 대각선 우선순위와 가로 검사 방향을 번갈아 적용하여
        // 모래 더미 전체가 한쪽으로만 밀리는 현상을 방지합니다.
        for (int y = 1; y < _height; y++)
        {
            int startX = preferLeftThisStep ? 0 : _width - 1;
            int endX = preferLeftThisStep ? _width : -1;
            int direction = preferLeftThisStep ? 1 : -1;

            for (int x = startX; x != endX; x += direction)
            {
                if (!IsOccupied(x, y))
                {
                    continue;
                }

                if (IsMoldCell(x, y))
                {
                    continue;
                }

                bool didMove = TryMove(x, y, x, y - 1);
                if (!didMove && preferLeftThisStep)
                {
                    didMove = TryMove(x, y, x - 1, y - 1) ||
                              TryMove(x, y, x + 1, y - 1);
                }
                else if (!didMove)
                {
                    didMove = TryMove(x, y, x + 1, y - 1) ||
                              TryMove(x, y, x - 1, y - 1);
                }

                if (didMove)
                {
                    moved = true;
                }
            }
        }

        if (moved)
        {
            _cellsDirty = true;
            _surfaceDirty = true;
        }
    }

    private bool TryMove(int fromX, int fromY, int toX, int toY)
    {
        if (!IsInside(toX, toY) || IsBlocked(toX, toY))
        {
            return false;
        }

        _cells[toY * _width + toX] = _cells[fromY * _width + fromX];
        _cells[fromY * _width + fromX] = default;
        return true;
    }

    private void BuildGrid()
    {
        Camera camera = Camera.main;
        float worldHeight;
        float worldWidth;
        Vector2 worldMin;

        if (_configuredBounds.HasValue)
        {
            Rect bounds = _configuredBounds.Value;
            worldWidth = bounds.width;
            worldHeight = bounds.height;
            worldMin = bounds.min;
        }
        else
        {
            worldHeight = camera != null && camera.orthographic
                ? camera.orthographicSize * 2f
                : 10f;
            worldWidth = camera != null
                ? worldHeight * camera.aspect
                : worldHeight * (16f / 9f);
            Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
            worldMin = new Vector2(
                cameraPosition.x - worldWidth * 0.5f,
                cameraPosition.y - worldHeight * 0.5f
            );
        }

        _width = Mathf.Min(MaxTextureSize, TargetGridWidth);
        _pixelsPerUnit = _width / worldWidth;
        _height = Mathf.Min(MaxTextureSize, Mathf.CeilToInt(worldHeight * _pixelsPerUnit));

        transform.position = new Vector3(worldMin.x, worldMin.y, 0f);

        _cells = new Color32[_width * _height];
        _displayCells = new Color32[_cells.Length];
        _moldCells = new Color32[_cells.Length];
        _guideCells = new Color32[_cells.Length];
        _templateCells = new bool[_cells.Length];
        _solidFloor = new int[_width];
        for (int i = 0; i < _solidFloor.Length; i++)
        {
            _solidFloor[i] = -1;
        }
        if (_renderer != null && _renderer.sprite != null)
        {
            Destroy(_renderer.sprite);
        }

        if (_texture != null)
        {
            Destroy(_texture);
        }

        _texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false)
        {
            name = "Sand Pile Texture",
            // 모래가 액체처럼 뭉개지지 않는 범위에서 가장자리를 부드럽게 표현합니다.
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _texture.SetPixels32(_cells);
        _texture.Apply(false, false);

        Sprite sprite = Sprite.Create(
            _texture,
            new Rect(0f, 0f, _width, _height),
            Vector2.zero,
            _pixelsPerUnit,
            0,
            SpriteMeshType.FullRect
        );

        if (_renderer == null)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
        }
        _renderer.sprite = sprite;
        _renderer.sortingOrder = -1;
        _renderer.enabled = _uiTarget == null;

        foreach (EdgeCollider2D surfaceCollider in _surfaceColliders)
        {
            surfaceCollider.enabled = false;
        }
    }

    private void BuildBoundsCollider(Vector2 size)
    {
        if (_boundsCollider == null)
        {
            _boundsCollider = gameObject.AddComponent<EdgeCollider2D>();
        }

        _boundsCollider.edgeRadius = 0.5f / _pixelsPerUnit;
        _boundsCollider.points = new[]
        {
            new Vector2(0f, size.y),
            Vector2.zero,
            new Vector2(size.x, 0f),
            new Vector2(size.x, size.y)
        };
        _boundsCollider.enabled = true;
    }

    private void BuildUiRenderer()
    {
        if (_uiTarget == null)
        {
            return;
        }

        if (_uiRenderer == null)
        {
            GameObject view = new GameObject(
                "Sand Pile View",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage)
            );
            view.layer = _uiTarget.gameObject.layer;
            RectTransform rect = (RectTransform)view.transform;
            rect.SetParent(_uiTarget, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            _uiRenderer = view.GetComponent<RawImage>();
            _uiRenderer.raycastTarget = false;
        }

        _uiRenderer.texture = _texture;
    }

    private void RebuildSurfaceColliders()
    {
        int colliderIndex = 0;
        int x = 0;

        while (x < _width)
        {
            while (x < _width && FindTopCell(x) < 0)
            {
                x++;
            }

            if (x >= _width)
            {
                break;
            }

            int start = x;
            while (x + 1 < _width && FindTopCell(x + 1) >= 0)
            {
                x++;
            }
            int end = x;

            EdgeCollider2D collider = GetSurfaceCollider(colliderIndex++);
            int pointCount = Mathf.Max(2, end - start + 1);
            Vector2[] points = new Vector2[pointCount];

            if (start == end)
            {
                float top = (FindTopCell(start) + 1f) / _pixelsPerUnit;
                points[0] = new Vector2(start / _pixelsPerUnit, top);
                points[1] = new Vector2((start + 1f) / _pixelsPerUnit, top);
            }
            else
            {
                for (int column = start; column <= end; column++)
                {
                    points[column - start] = new Vector2(
                        (column + 0.5f) / _pixelsPerUnit,
                        (FindTopCell(column) + 1f) / _pixelsPerUnit
                    );
                }
            }

            collider.points = points;
            collider.enabled = true;
            x++;
        }

        for (int i = colliderIndex; i < _surfaceColliders.Count; i++)
        {
            _surfaceColliders[i].enabled = false;
        }

        // 별도의 표면 장식은 완성된 그림을 지저분하게 만들 수 있어 사용하지 않습니다.
        // 대신 모래 텍스처 자체에 은은한 모래결을 적용합니다.
    }

    private EdgeCollider2D GetSurfaceCollider(int index)
    {
        if (index < _surfaceColliders.Count)
        {
            return _surfaceColliders[index];
        }

        EdgeCollider2D collider = gameObject.AddComponent<EdgeCollider2D>();
        collider.edgeRadius = 0.5f / _pixelsPerUnit;
        _surfaceColliders.Add(collider);
        return collider;
    }

    private int FindTopCell(int x)
    {
        for (int y = _height - 1; y >= 0; y--)
        {
            if (IsOccupied(x, y))
            {
                return y;
            }
        }

        return -1;
    }

    private Vector2Int WorldToCell(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - transform.position;
        return new Vector2Int(
            Mathf.FloorToInt(local.x * _pixelsPerUnit),
            Mathf.FloorToInt(local.y * _pixelsPerUnit)
        );
    }

    private bool IsInside(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    private bool IsOccupied(int x, int y)
    {
        return IsInside(x, y) &&
               (_cells[y * _width + x].a != 0 || _moldCells[y * _width + x].a != 0);
    }

    private bool IsMoldCell(int x, int y)
    {
        return IsInside(x, y) && _moldCells[y * _width + x].a != 0;
    }

    private bool IsBlocked(int x, int y)
    {
        return y <= _solidFloor[x] || IsOccupied(x, y);
    }

    private void SetCell(int x, int y, Color32 color)
    {
        _cells[y * _width + x] = color;
    }

    private void UpdateDisplayTexture()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int index = y * _width + x;
                // The outline is a tracing overlay: keep it visible even after sand covers
                // the same cell, while leaving collision and simulation unchanged.
                Color32 source = _guideCells[index].a != 0
                    ? _guideCells[index]
                    : (_cells[index].a != 0 ? _cells[index] : _moldCells[index]);
                if (source.a == 0)
                {
                    _displayCells[index] = default;
                    continue;
                }

                int fineHash = x * 73856093 ^ y * 19349663;
                float fineGrain = ((fineHash & 255) / 255f - 0.5f) * 0.07f;
                float softClump = Mathf.PerlinNoise(x * 0.085f, y * 0.085f) * 0.06f - 0.03f;
                bool surfaceExposed = !IsOccupied(x, y + 1);

                // 규칙적인 명암 띠 대신 미세 알갱이와 작은 모래 덩어리의 불규칙한 질감을 섞습니다.
                // 가장 위쪽 표면에만 약한 빛을 더해 자연스러운 모래 더미처럼 표현합니다.
                float depthShade = 1f + fineGrain + softClump;
                if (surfaceExposed)
                {
                    depthShade += 0.035f;
                }

                _displayCells[index] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(source.r * depthShade), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(source.g * depthShade), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(source.b * depthShade), 0, 255),
                    source.a
                );
            }
        }
    }


    private void BuildSurfaceDetailPool()
    {
        const int textureSize = 16;
        _detailTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "Sand Surface Grain",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[textureSize * textureSize];
        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 delta = new Vector2(x, y) - center;
                float angleNoise = Mathf.Sin(Mathf.Atan2(delta.y, delta.x) * 5f) * 0.65f;
                float radius = textureSize * 0.38f + angleNoise;
                float alpha = Mathf.Clamp01(radius - delta.magnitude + 0.75f);
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        _detailTexture.SetPixels32(pixels);
        _detailTexture.Apply(false, false);
        _detailSprite = Sprite.Create(
            _detailTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize
        );

        GameObject container = new GameObject("Sand Surface Details");
        container.transform.SetParent(transform, false);
        _surfaceDetailContainer = container.transform;

        for (int i = 0; i < MaxSurfaceDetails; i++)
        {
            GameObject detail = new GameObject("Surface Grain");
            detail.transform.SetParent(_surfaceDetailContainer, false);
            SpriteRenderer detailRenderer = detail.AddComponent<SpriteRenderer>();
            detailRenderer.sprite = _detailSprite;
            detailRenderer.sortingOrder = 1;
            detail.SetActive(false);
            _surfaceDetails.Add(detailRenderer);
        }
    }

    private void RefreshSurfaceDetails()
    {
        int detailIndex = 0;
        int stride = Mathf.Max(1, Mathf.CeilToInt(_width / (float)MaxSurfaceDetails));
        float cellSize = 1f / _pixelsPerUnit;

        for (int x = 0; x < _width && detailIndex < _surfaceDetails.Count; x += stride)
        {
            int top = FindTopCell(x);
            if (top < 0)
            {
                continue;
            }

            SpriteRenderer detail = _surfaceDetails[detailIndex++];
            int hash = x * 73856093 ^ top * 19349663;
            float jitterX = (((hash & 255) / 255f) - 0.5f) * 0.4f;
            float jitterY = ((((hash >> 8) & 255) / 255f) - 0.5f) * 0.2f;
            float scale = Mathf.Lerp(0.8f, 1.2f, ((hash >> 16) & 255) / 255f);

            detail.transform.localPosition = new Vector3(
                (x + 0.5f + jitterX) * cellSize,
                (top + 1f + jitterY) * cellSize,
                0f
            );
            detail.transform.localRotation = Quaternion.Euler(0f, 0f, hash % 360);
            detail.transform.localScale = Vector3.one * (cellSize * scale);
            detail.color = _cells[top * _width + x];
            detail.gameObject.SetActive(true);
        }

        for (int i = detailIndex; i < _surfaceDetails.Count; i++)
        {
            _surfaceDetails[i].gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        if (_renderer != null && _renderer.sprite != null)
        {
            Destroy(_renderer.sprite);
        }

        if (_texture != null)
        {
            Destroy(_texture);
        }

        if (_detailSprite != null)
        {
            Destroy(_detailSprite);
        }

        if (_detailTexture != null)
        {
            Destroy(_detailTexture);
        }
    }
}
