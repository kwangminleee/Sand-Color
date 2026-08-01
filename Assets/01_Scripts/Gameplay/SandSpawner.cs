using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public sealed class SandSpawner : MonoBehaviour
{
    [Header("Touch Area")]
    [SerializeField] private RectTransform _touchArea;

    [Header("모래 입자")]
    [SerializeField] private SandParticle _particlePrefab;
    [SerializeField] private Color[] _sandColors =
    {
        new Color(1f, 0.48f, 0.62f, 1f),
        new Color(1f, 0.62f, 0.25f, 1f),
        new Color(1f, 0.86f, 0.28f, 1f),
        new Color(0.43f, 0.82f, 0.98f, 1f),
        new Color(0.52f, 0.88f, 0.72f, 1f),
        new Color(0.67f, 0.55f, 0.93f, 1f),
        new Color(0.98f, 0.43f, 0.35f, 1f),
        new Color(0.32f, 0.82f, 0.83f, 1f)
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

    private sealed class ParticleView
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

    public event Action<Color, Color, int> ColorsChanged;
    public Color CurrentColor => _currentColor;
    public Color NextColor => _nextColor;
    public int RemainingParticles => _remainingParticles;

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
        SandPileController pileController = SandPileController.Instance;
        pileController.SetSimulationStepsPerFrame(_sandStepsPerFrame);

        if (_touchArea != null && TryGetTouchAreaWorldBounds(out Rect worldBounds))
        {
            _spawnWorldBounds = worldBounds;
            _hasSpawnWorldBounds = true;
            pileController.ConfigureBounds(_touchArea, worldBounds);
        }

        SandParticlePool();
        InitializeColorQueue();
        _initialized = enabled;
    }

    private void Update()
    {
        if (_initialized && TryGetPointerWorldPosition(out Vector3 emissionPosition))
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
        if (SandPileController.Instance.ContainsSand(worldPosition))
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
}
