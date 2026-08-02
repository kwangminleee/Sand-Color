using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SandSpawner : MonoBehaviour
{
    [Header("Scene Components")]
    [SerializeField] private SandPileController _sandPileController;

    [Header("Touch Area")]
    [SerializeField] private RectTransform _touchArea;

    [Header("모래 입자")]
    [SerializeField] private SandParticle _particlePrefab;
    [SerializeField] private Color[] _sandColors =
    {
        // 고래 레퍼런스에서 추출한 크림 / 산호 / 분홍 계열
        new Color(0.9922f, 0.9255f, 0.7843f, 1f),
        new Color(0.9922f, 0.9020f, 0.7255f, 1f),
        new Color(0.9922f, 0.8784f, 0.6667f, 1f),
        new Color(0.9922f, 0.8549f, 0.6667f, 1f),
        new Color(0.9922f, 0.8275f, 0.6588f, 1f),
        new Color(0.9882f, 0.8000f, 0.6784f, 1f),
        new Color(0.9882f, 0.7686f, 0.6471f, 1f),
        new Color(0.9882f, 0.7451f, 0.6471f, 1f),
        new Color(0.9843f, 0.7137f, 0.6510f, 1f),
        new Color(0.9686f, 0.6667f, 0.6667f, 1f),
        new Color(0.9647f, 0.6471f, 0.6627f, 1f),
        new Color(0.9216f, 0.7020f, 0.7333f, 1f),
        new Color(0.9020f, 0.7529f, 0.7725f, 1f),
        new Color(0.8980f, 0.6196f, 0.7176f, 1f),
        new Color(0.8863f, 0.6510f, 0.7608f, 1f),
        new Color(0.8471f, 0.5725f, 0.7020f, 1f),

        // 보라 / 라벤더 / 하늘 계열
        new Color(0.7882f, 0.7255f, 0.8039f, 1f),
        new Color(0.7529f, 0.6549f, 0.8118f, 1f),
        new Color(0.7451f, 0.5765f, 0.7882f, 1f),
        new Color(0.7059f, 0.4863f, 0.7412f, 1f),
        new Color(0.6902f, 0.6510f, 0.8039f, 1f),
        new Color(0.6196f, 0.6118f, 0.7961f, 1f),
        new Color(0.5686f, 0.7176f, 0.8392f, 1f),
        new Color(0.5686f, 0.4196f, 0.7451f, 1f),
        new Color(0.5216f, 0.4902f, 0.7804f, 1f),
        new Color(0.4902f, 0.5843f, 0.7961f, 1f),
        new Color(0.4824f, 0.4000f, 0.7412f, 1f),
        new Color(0.4353f, 0.3725f, 0.7294f, 1f),

        // 고래 몸통의 밝은 회청색부터 짙은 남색까지
        new Color(0.9255f, 0.8549f, 0.7765f, 1f),
        new Color(0.8902f, 0.8157f, 0.7725f, 1f),
        new Color(0.8510f, 0.7490f, 0.7647f, 1f),
        new Color(0.7490f, 0.7294f, 0.7843f, 1f),
        new Color(0.6706f, 0.6314f, 0.7569f, 1f),
        new Color(0.5255f, 0.6000f, 0.7608f, 1f),
        new Color(0.4431f, 0.5294f, 0.7294f, 1f),
        new Color(0.4235f, 0.4784f, 0.7020f, 1f),
        new Color(0.3608f, 0.4588f, 0.6863f, 1f),
        new Color(0.3176f, 0.4353f, 0.6706f, 1f),
        new Color(0.3059f, 0.3255f, 0.6667f, 1f),
        new Color(0.2667f, 0.4196f, 0.6431f, 1f),
        new Color(0.2471f, 0.3882f, 0.6275f, 1f),
        new Color(0.2314f, 0.3686f, 0.6078f, 1f),
        new Color(0.1961f, 0.3373f, 0.5765f, 1f),
        new Color(0.1686f, 0.3294f, 0.5569f, 1f),
        new Color(0.1569f, 0.2980f, 0.5373f, 1f),
        new Color(0.1216f, 0.2549f, 0.4824f, 1f),
        new Color(0.08f, 0.14f, 0.27f, 1f),
        new Color(0.03f, 0.05f, 0.10f, 1f)
    };
    [SerializeField, Min(1)] private int _particlesPerColor = 1200;
    [SerializeField, Min(16)] private int _poolSize = 192;

    [Header("스폰 설정")]
    [SerializeField, Min(1f)] private float _particlesPerSecond = 180f;
    [SerializeField, Min(0f)] private float _streamWidth = 0.12f;
    [SerializeField, Min(0f)] private float _spawnEdgePadding = 0.08f;
    [SerializeField] private Vector2 _horizontalSpeedRange = new Vector2(-0.25f, 0.25f);
    [SerializeField] private Vector2 _downwardSpeedRange = new Vector2(0.35f, 0.8f);
    [SerializeField, Range(1, 8)] private int _sandStepsPerFrame = 4;
    [SerializeField, Min(2f)] private float _uiParticleSize = 18f;

    private class ParticleView
    {
        public SandParticle Particle;
        public RectTransform Rect;
        public Image Image;
    }

    private readonly Queue<SandParticle> _availableParticles =
        new Queue<SandParticle>();
    private readonly List<ParticleView> _particleViews =
        new List<ParticleView>();
    private Camera _camera;
    private Camera _uiCamera;
    private float _emissionAccumulator;
    private bool _initialized;
    private Rect _spawnWorldBounds;
    private bool _hasSpawnWorldBounds;
    private Transform _particleContainer;
    private RectTransform _particleViewContainer;
    private Color _currentColor;
    private Color _nextColor;
    private int _remainingParticles;
    private bool _inputEnabled = true;

#if UNITY_EDITOR
    private struct DebugPaintDrop
    {
        public float NormalizedX;
        public Color Color;
        public int Row;
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;
    }

    private readonly Queue<DebugPaintDrop> _debugPaintDrops = new Queue<DebugPaintDrop>();
    private readonly Dictionary<SandParticle, DebugPaintDrop> _activeDebugDrops =
        new Dictionary<SandParticle, DebugPaintDrop>();
    private float _debugDropsPerSecond;
    private float _debugPaintAccumulator;
    private int _debugTotalDrops;
    private int _debugCurrentRow = -1;
    private int _debugActiveParticles;
#endif

    public event Action<Color, Color, int> ColorsChanged;
    public Color CurrentColor => _currentColor;
    public Color NextColor => _nextColor;
    public int RemainingParticles => _remainingParticles;
    public bool InputEnabled => _inputEnabled;
    public bool IsInitialized => _initialized;

    public void SetInputEnabled(bool inputEnabled)
    {
        _inputEnabled = inputEnabled;
        if (!inputEnabled)
        {
            _emissionAccumulator = 0f;
        }
    }

    /// <summary>
    /// 인스펙터에 저장된 기본 팔레트는 유지하면서 실행 중 팔레트만 교체합니다.
    /// Start 전에 호출하면 첫 번째 색상부터 해당 스테이지 팔레트를 사용합니다.
    /// </summary>
    public void SetSandPalette(Color[] colors)
    {
        if (colors == null || colors.Length == 0)
        {
            return;
        }

        _sandColors = new Color[colors.Length];
        for (int index = 0; index < colors.Length; index++)
        {
            Color color = colors[index];
            color.a = 1f;
            _sandColors[index] = color;
        }

        if (_initialized)
        {
            InitializeColorQueue();
        }
    }

    private void Awake()
    {
        _camera = Camera.main;
        if (_touchArea == null)
        {
            _touchArea = transform as RectTransform;
        }

        Canvas canvas = _touchArea != null
            ? _touchArea.GetComponentInParent<Canvas>()
            : null;
        _uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }

    private void Start()
    {
        if (_sandPileController == null)
        {
            Debug.LogError(
                "SandSpawner의 Sand Pile Controller를 Inspector에서 할당하세요.",
                this);
            enabled = false;
            return;
        }

        _sandPileController.SetSimulationStepsPerFrame(_sandStepsPerFrame);

        if (_touchArea != null && TryGetTouchAreaWorldBounds(out Rect worldBounds))
        {
            _spawnWorldBounds = worldBounds;
            _hasSpawnWorldBounds = true;
            _sandPileController.ConfigureBounds(_touchArea, worldBounds);
        }

        SandParticlePool();
        InitializeColorQueue();
        _initialized = enabled;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (_debugPaintDrops.Count > 0)
        {
            UpdateDebugPainting();
            return;
        }
#endif

        if (_initialized && _inputEnabled &&
            TryGetPointerWorldPosition(out Vector3 emissionPosition))
        {
            EmitParticles(emissionPosition);
        }
        else
        {
            _emissionAccumulator = 0f;
        }
    }

    private void LateUpdate()
    {
        if (_touchArea == null || _camera == null)
        {
            return;
        }

        for (int i = 0; i < _particleViews.Count; i++)
        {
            ParticleView view = _particleViews[i];
            bool visible = view.Particle != null && view.Particle.IsSpawned;
            view.Image.enabled = visible;
            if (!visible)
            {
                continue;
            }

            Vector2 screenPoint = _camera.WorldToScreenPoint(view.Particle.transform.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _touchArea,
                    screenPoint,
                    _uiCamera,
                    out Vector2 localPoint))
            {
                view.Image.enabled = false;
                continue;
            }

            view.Rect.anchoredPosition = localPoint;
            Vector2 visualScale = view.Particle.VisualScale;
            view.Rect.localScale = new Vector3(visualScale.x, visualScale.y, 1f);
            view.Image.color = view.Particle.RenderColor;
        }
    }

    private bool TryGetTouchAreaWorldBounds(out Rect worldBounds)
    {
        worldBounds = default;
        if (_touchArea == null || _camera == null)
        {
            return false;
        }

        Vector3[] corners = new Vector3[4];
        _touchArea.GetWorldCorners(corners);
        Plane gameplayPlane = new Plane(Vector3.forward, transform.position);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_uiCamera, corners[i]);
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            if (!gameplayPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            Vector2 point = ray.GetPoint(distance);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        worldBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return worldBounds.width > 0f && worldBounds.height > 0f;
    }

    private void SandParticlePool()
    {
        if (_particlePrefab == null)
        {
            Debug.LogError($"{nameof(SandSpawner)}에 모래 입자 프리팹이 필요합니다.", this);
            enabled = false;
            return;
        }

        GameObject particleContainer = new GameObject("Sand Particles");
        particleContainer.transform.SetParent(transform, false);
        _particleContainer = particleContainer.transform;

        if (_touchArea != null)
        {
            GameObject viewContainer = new GameObject("Falling Sand Views", typeof(RectTransform));
            viewContainer.layer = _touchArea.gameObject.layer;
            _particleViewContainer = (RectTransform)viewContainer.transform;
            _particleViewContainer.SetParent(_touchArea, false);
            _particleViewContainer.anchorMin = Vector2.zero;
            _particleViewContainer.anchorMax = Vector2.one;
            _particleViewContainer.offsetMin = Vector2.zero;
            _particleViewContainer.offsetMax = Vector2.zero;
            _particleViewContainer.SetAsLastSibling();
        }

        int count = Mathf.Max(16, _poolSize);
        for (int i = 0; i < count; i++)
        {
            // TouchArea는 UI 트랜스폼이므로 물리 입자는 월드 공간에 유지합니다.
            SandParticle particle = Instantiate(_particlePrefab, _particleContainer);
            particle.SetSandPileController(_sandPileController);
            particle.OnDespawned();
            _availableParticles.Enqueue(particle);
            CreateParticleView(particle);
        }
    }

    private void CreateParticleView(SandParticle particle)
    {
        if (_touchArea == null || particle == null)
        {
            return;
        }

        GameObject viewObject = new GameObject(
            "Falling Sand",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        viewObject.layer = _touchArea.gameObject.layer;

        RectTransform rect = (RectTransform)viewObject.transform;
        rect.SetParent(_particleViewContainer != null ? _particleViewContainer : _touchArea, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * _uiParticleSize;
        rect.SetAsLastSibling();

        Image image = viewObject.GetComponent<Image>();
        image.sprite = particle.ParticleSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;

        _particleViews.Add(new ParticleView
        {
            Particle = particle,
            Rect = rect,
            Image = image
        });
    }

    private bool TryGetPointerWorldPosition(out Vector3 worldPosition)
    {
        Vector2 screenPosition;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                worldPosition = default;
                return false;
            }

            screenPosition = touch.position;
        }
        else if (Input.GetMouseButton(0))
        {
            screenPosition = Input.mousePosition;
        }
        else
        {
            worldPosition = default;
            return false;
        }

        if (_touchArea != null &&
            !RectTransformUtility.RectangleContainsScreenPoint(
                _touchArea,
                screenPosition,
                _uiCamera))
        {
            worldPosition = default;
            return false;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_camera == null)
        {
            worldPosition = default;
            return false;
        }

        Ray pointerRay = _camera.ScreenPointToRay(screenPosition);
        Plane gameplayPlane = new Plane(Vector3.forward, transform.position);
        if (!gameplayPlane.Raycast(pointerRay, out float distance))
        {
            worldPosition = default;
            return false;
        }

        worldPosition = pointerRay.GetPoint(distance);
        if (_sandPileController != null && _sandPileController.ContainsSand(worldPosition))
        {
            worldPosition = default;
            return false;
        }

        return true;
    }

    private void EmitParticles(Vector3 emissionPosition)
    {
        _emissionAccumulator += Time.deltaTime * Mathf.Max(1f, _particlesPerSecond);
        int emitCount = Mathf.FloorToInt(_emissionAccumulator);
        _emissionAccumulator -= emitCount;

        for (int i = 0; i < emitCount && _availableParticles.Count > 0; i++)
        {
            SandParticle particle = _availableParticles.Dequeue();
            Vector2 circularOffset = Random.insideUnitCircle * _streamWidth;
            Vector3 spawnPosition = emissionPosition + new Vector3(
                circularOffset.x,
                circularOffset.y,
                0f
            );
            if (_hasSpawnWorldBounds)
            {
                float horizontalPadding = Mathf.Min(
                    _spawnEdgePadding,
                    _spawnWorldBounds.width * 0.5f
                );
                float verticalPadding = Mathf.Min(
                    _spawnEdgePadding,
                    _spawnWorldBounds.height * 0.5f
                );
                spawnPosition.x = Mathf.Clamp(
                    spawnPosition.x,
                    _spawnWorldBounds.xMin + horizontalPadding,
                    _spawnWorldBounds.xMax - horizontalPadding
                );
                spawnPosition.y = Mathf.Clamp(
                    spawnPosition.y,
                    _spawnWorldBounds.yMin + verticalPadding,
                    _spawnWorldBounds.yMax - verticalPadding
                );
            }

            Vector2 velocity = new Vector2(
                Random.Range(_horizontalSpeedRange.x, _horizontalSpeedRange.y),
                -Random.Range(_downwardSpeedRange.x, _downwardSpeedRange.y)
            );

            particle.Emit(spawnPosition, velocity, _currentColor, RecycleParticle);
            _remainingParticles--;
            if (_remainingParticles <= 0)
            {
                AdvanceColor();
            }
        }
    }

    public void SkipColor()
    {
        if (!_initialized)
        {
            return;
        }

        AdvanceColor();
        _emissionAccumulator = 0f;
    }

    private void InitializeColorQueue()
    {
        _currentColor = PickRandomColor();
        _nextColor = PickRandomColor();
        _remainingParticles = Mathf.Max(1, _particlesPerColor);
        NotifyColorsChanged();
    }

    private void AdvanceColor()
    {
        _currentColor = _nextColor;
        _nextColor = PickRandomColor();
        _remainingParticles = Mathf.Max(1, _particlesPerColor);
        NotifyColorsChanged();
    }

    private Color PickRandomColor()
    {
        if (_sandColors == null || _sandColors.Length == 0)
        {
            return new Color(1f, 0.72f, 0.18f, 1f);
        }

        return _sandColors[UnityEngine.Random.Range(0, _sandColors.Length)];
    }

    private void NotifyColorsChanged()
    {
        ColorsChanged?.Invoke(_currentColor, _nextColor, _remainingParticles);
    }

    private void RecycleParticle(SandParticle particle)
    {
        if (particle != null)
        {
            _availableParticles.Enqueue(particle);
        }
    }

#if UNITY_EDITOR
    public bool IsDebugPainting => _debugPaintDrops.Count > 0;
    public float DebugPaintProgress => _debugTotalDrops <= 0
        ? 0f
        : 1f - _debugPaintDrops.Count / (float)_debugTotalDrops;

    public void DebugBuildTemplateMold(
        Color32[] mask,
        int maskWidth,
        int maskHeight,
        Color color)
    {
        _sandPileController?.BuildTemplateMold(mask, maskWidth, maskHeight, color);
    }

    public void DebugBuildTemplateOutline(
        Color32[] mask,
        int maskWidth,
        int maskHeight,
        Color color,
        int thickness)
    {
        _sandPileController?.BuildTemplateOutline(
            mask,
            maskWidth,
            maskHeight,
            color,
            thickness);
    }

    public void DebugClearTemplate()
    {
        _sandPileController?.ClearTemplate();
    }

    public void StartDebugPainting(
        Color32[] pixels,
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        float dropsPerSecond,
        bool useGamePalette)
    {
        if (!_initialized || pixels == null || pixels.Length != sourceWidth * sourceHeight)
        {
            return;
        }

        _debugPaintDrops.Clear();
        _activeDebugDrops.Clear();
        SandPileController pileController = _sandPileController;
        if (pileController == null)
        {
            return;
        }
        pileController.ClearSand();
        pileController.ClearTemplate();

        int width = Mathf.Clamp(targetWidth, 16, 320);
        int height = Mathf.Max(1, Mathf.RoundToInt(width * sourceHeight / (float)sourceWidth));
        int gridWidth = pileController.GridWidth;
        int gridHeight = pileController.GridHeight;

        // 아래 행부터 좌우 방향을 번갈아 이동하며 실제 플레이처럼 모래를 떨어뜨립니다.
        for (int y = 0; y < height; y++)
        {
            bool reverse = (y & 1) == 1;
            for (int column = 0; column < width; column++)
            {
                int x = reverse ? width - 1 - column : column;
                int sourceX = Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) / width * sourceWidth), 0, sourceWidth - 1);
                int sourceY = Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) / height * sourceHeight), 0, sourceHeight - 1);
                Color32 color = pixels[sourceY * sourceWidth + sourceX];
                if (color.a < 16)
                {
                    continue;
                }

                _debugPaintDrops.Enqueue(new DebugPaintDrop
                {
                    NormalizedX = (x + 0.5f) / width,
                    Color = useGamePalette ? FindClosestGameColor(color) : color,
                    Row = y,
                    MinX = Mathf.FloorToInt(x / (float)width * gridWidth),
                    MaxX = Mathf.CeilToInt((x + 1f) / width * gridWidth),
                    MinY = Mathf.FloorToInt(y / (float)height * gridHeight),
                    MaxY = Mathf.CeilToInt((y + 1f) / height * gridHeight)
                });
            }
        }

        _debugDropsPerSecond = Mathf.Max(1f, dropsPerSecond);

        _debugPaintAccumulator = 0f;
        _debugTotalDrops = _debugPaintDrops.Count;
        _debugCurrentRow = -1;
        _debugActiveParticles = 0;
    }

    public void StopDebugPainting()
    {
        _debugPaintDrops.Clear();
        _debugTotalDrops = 0;
        _debugPaintAccumulator = 0f;
        _debugCurrentRow = -1;
    }

    private Color32 FindClosestGameColor(Color32 source)
    {
        if (_sandColors == null || _sandColors.Length == 0)
        {
            return source;
        }

        Color sourceLinear = ((Color)source).linear;
        Color closest = _sandColors[0];
        float closestDistance = float.PositiveInfinity;
        foreach (Color paletteColor in _sandColors)
        {
            Color candidate = paletteColor.linear;
            float red = sourceLinear.r - candidate.r;
            float green = sourceLinear.g - candidate.g;
            float blue = sourceLinear.b - candidate.b;
            float distance = red * red * 0.3f + green * green * 0.59f + blue * blue * 0.11f;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = paletteColor;
            }
        }

        closest.a = source.a / 255f;
        return closest;
    }

    private void UpdateDebugPainting()
    {
        if (!_hasSpawnWorldBounds)
        {
            StopDebugPainting();
            return;
        }

        _debugPaintAccumulator += Time.deltaTime * _debugDropsPerSecond;
        int requestedDrops = Mathf.Min(3, Mathf.FloorToInt(_debugPaintAccumulator));
        int emittedDrops = 0;

        if (_debugActiveParticles == 0 && _debugPaintDrops.Count > 0)
        {
            _debugCurrentRow = _debugPaintDrops.Peek().Row;
        }

        while (emittedDrops < requestedDrops &&
               _debugPaintDrops.Count > 0 &&
               _availableParticles.Count > 0 &&
               _debugPaintDrops.Peek().Row == _debugCurrentRow)
        {
            DebugPaintDrop drop = _debugPaintDrops.Dequeue();
            SandParticle particle = _availableParticles.Dequeue();
            _activeDebugDrops[particle] = drop;
            float x = Mathf.Lerp(_spawnWorldBounds.xMin, _spawnWorldBounds.xMax, drop.NormalizedX);
            Vector3 position = new Vector3(
                x,
                _spawnWorldBounds.yMax - _spawnEdgePadding,
                transform.position.z
            );
            particle.Emit(
                position,
                Vector2.down * _downwardSpeedRange.y,
                drop.Color,
                RecycleDebugParticle,
                0,
                SettleDebugParticle
            );
            _debugActiveParticles++;
            emittedDrops++;
        }

        _debugPaintAccumulator = Mathf.Max(0f, _debugPaintAccumulator - emittedDrops);
    }

    private void RecycleDebugParticle(SandParticle particle)
    {
        _activeDebugDrops.Remove(particle);
        _debugActiveParticles = Mathf.Max(0, _debugActiveParticles - 1);
        RecycleParticle(particle);
    }

    private void SettleDebugParticle(SandParticle particle)
    {
        if (!_activeDebugDrops.TryGetValue(particle, out DebugPaintDrop drop))
        {
            return;
        }

        _sandPileController?.PaintDebugBlock(
            drop.MinX,
            drop.MaxX,
            drop.MinY,
            drop.MaxY,
            drop.Color);
    }
#endif
}
