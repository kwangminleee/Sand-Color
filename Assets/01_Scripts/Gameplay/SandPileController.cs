using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물리 모래 입자가 바닥에 닿으면 고정 크기 격자의 모래로 변환하고,
/// 격자 안에서 아래 또는 대각선 방향으로 흐르며 쌓이도록 관리합니다.
/// </summary>
public sealed class SandPileController : MonoBehaviour
{
    private const int TargetGridWidth = 256;
    private const int MaxSurfaceDetails = 128;
    private const int MaxTextureSize = 1024;

    private static SandPileController _instance;

    private readonly List<EdgeCollider2D> _surfaceColliders = new List<EdgeCollider2D>();
    private readonly List<SpriteRenderer> _surfaceDetails = new List<SpriteRenderer>();
    private Texture2D _texture;
    private Texture2D _detailTexture;
    private Sprite _detailSprite;
    private Color32[] _cells;
    private int[] _solidFloor;
    private SpriteRenderer _renderer;
    private int _width;
    private int _height;
    private float _pixelsPerUnit;
    private int _simulationStepsPerFrame = 3;
    private bool _preferLeft = true;
    private bool _cellsDirty;
    private bool _surfaceDirty;

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
            _texture.SetPixels32(_cells);
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
        float brightness = Random.Range(0.92f, 1.08f);
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
        float worldHeight = camera != null && camera.orthographic
            ? camera.orthographicSize * 2f
            : 10f;
        float worldWidth = camera != null
            ? worldHeight * camera.aspect
            : worldHeight * (16f / 9f);

        _width = Mathf.Min(MaxTextureSize, TargetGridWidth);
        _pixelsPerUnit = _width / worldWidth;
        _height = Mathf.Min(MaxTextureSize, Mathf.CeilToInt(worldHeight * _pixelsPerUnit));

        Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
        transform.position = new Vector3(
            cameraPosition.x - _width / _pixelsPerUnit * 0.5f,
            cameraPosition.y - _height / _pixelsPerUnit * 0.5f,
            0f
        );

        _cells = new Color32[_width * _height];
        _solidFloor = new int[_width];
        for (int i = 0; i < _solidFloor.Length; i++)
        {
            _solidFloor[i] = -1;
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

        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = sprite;
        _renderer.sortingOrder = -1;

        BuildSurfaceDetailPool();
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

        RefreshSurfaceDetails();
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
        return IsInside(x, y) && _cells[y * _width + x].a != 0;
    }

    private bool IsBlocked(int x, int y)
    {
        return y <= _solidFloor[x] || IsOccupied(x, y);
    }

    private void SetCell(int x, int y, Color32 color)
    {
        _cells[y * _width + x] = color;
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

        for (int i = 0; i < MaxSurfaceDetails; i++)
        {
            GameObject detail = new GameObject("Surface Grain");
            detail.transform.SetParent(transform, false);
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
