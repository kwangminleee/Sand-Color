using System.Collections.Generic;
using UnityEngine;

public sealed class SandSpawner : MonoBehaviour
{
    [Header("모래 입자")]
    [SerializeField] private SandParticle _particlePrefab;
    [SerializeField] private Color _sandColor = new Color(1f, 0.72f, 0.18f, 1f);
    [SerializeField, Min(16)] private int _poolSize = 192;

    [Header("스폰 설정")]
    [SerializeField, Min(1f)] private float _particlesPerSecond = 180f;
    [SerializeField, Min(0f)] private float _streamWidth = 0.12f;
    [SerializeField] private Vector2 _horizontalSpeedRange = new Vector2(-0.25f, 0.25f);
    [SerializeField] private Vector2 _downwardSpeedRange = new Vector2(0.35f, 0.8f);
    [SerializeField, Range(1, 8)] private int _sandStepsPerFrame = 4;

    private readonly Queue<SandParticle> _availableParticles =
        new Queue<SandParticle>();
    private Camera _camera;
    private float _emissionAccumulator;

    private void Awake()
    {
        SandPileController.Instance.SetSimulationStepsPerFrame(_sandStepsPerFrame);
        _camera = Camera.main;
        SandParticlePool();
    }

    private void Update()
    {
        if (TryGetPointerWorldPosition(out Vector3 emissionPosition))
        {
            EmitParticles(emissionPosition);
        }
        else
        {
            _emissionAccumulator = 0f;
        }
    }

    private void SandParticlePool()
    {
        if (_particlePrefab == null)
        {
            Debug.LogError($"{nameof(SandSpawner)}에 모래 입자 프리팹이 필요합니다.", this);
            enabled = false;
            return;
        }

        int count = Mathf.Max(16, _poolSize);
        for (int i = 0; i < count; i++)
        {
            SandParticle particle = Instantiate(_particlePrefab, transform);
            particle.gameObject.SetActive(false);
            _availableParticles.Enqueue(particle);
        }
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
            Vector3 spawnPosition = emissionPosition +
                Vector3.right * Random.Range(-_streamWidth, _streamWidth);
            Vector2 velocity = new Vector2(
                Random.Range(_horizontalSpeedRange.x, _horizontalSpeedRange.y),
                -Random.Range(_downwardSpeedRange.x, _downwardSpeedRange.y)
            );

            particle.Emit(spawnPosition, velocity, _sandColor, RecycleParticle);
        }
    }

    private void RecycleParticle(SandParticle particle)
    {
        if (particle != null)
        {
            _availableParticles.Enqueue(particle);
        }
    }
}
