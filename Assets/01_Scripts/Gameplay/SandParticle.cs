using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class SandParticle : MonoBehaviour, IPoolable
{
    [Header("입자 외형")]
    [SerializeField] private Vector2 _scaleRange = new Vector2(0.72f, 1f);
    [SerializeField] private Vector2 _flatnessRange = new Vector2(0.55f, 0.82f);
    [SerializeField, Range(0f, 0.25f)] private float _colorVariation = 0.09f;

    private Rigidbody2D _body;
    private Collider2D _collider;
    private SpriteRenderer _sprite;
    private Action<SandParticle> _recycle;
    private Action<SandParticle> _settleOverride;
    private Vector3 _baseScale;
    private bool _settled;
    private int _settleAmount;

    public Sprite ParticleSprite => _sprite != null ? _sprite.sprite : null;
    public Color RenderColor => _sprite != null ? _sprite.color : Color.white;
    public bool IsSpawned => gameObject.activeSelf && _body != null && _body.simulated;
    public Vector2 VisualScale => new Vector2(
        _baseScale.x != 0f ? transform.localScale.x / _baseScale.x : 1f,
        _baseScale.y != 0f ? transform.localScale.y / _baseScale.y : 1f
    );

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        TryGetComponent(out _sprite);
        _baseScale = transform.localScale;
    }

    public void Emit(
        Vector3 position,
        Vector2 velocity,
        Color color,
        Action<SandParticle> recycle,
        int settleAmount = 0,
        Action<SandParticle> settleOverride = null)
    {
        _recycle = recycle;
        _settleOverride = settleOverride;
        _settleAmount = settleAmount;
        _settled = false;
        transform.SetPositionAndRotation(
            position,
            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
        );

        float scale = Random.Range(_scaleRange.x, _scaleRange.y);
        float flatness = Random.Range(_flatnessRange.x, _flatnessRange.y);
        transform.localScale = Vector3.Scale(
            _baseScale,
            new Vector3(scale, scale * flatness, 1f)
        );

        if (_sprite != null)
        {
            float shade = Random.Range(-_colorVariation, _colorVariation);
            _sprite.color = new Color(
                Mathf.Clamp01(color.r + shade),
                Mathf.Clamp01(color.g + shade * 0.75f),
                Mathf.Clamp01(color.b + shade * 0.4f),
                color.a
            );
            _sprite.sortingOrder = Random.Range(0, 4);
        }

        OnSpawned();
        _body.velocity = velocity;
        _body.angularVelocity = Random.Range(-180f, 180f);
    }

    public void OnSpawned()
    {
        gameObject.SetActive(true);
        _collider.enabled = true;
        _body.simulated = true;
    }

    public void OnDespawned()
    {
        _collider.enabled = false;
        _body.simulated = false;
        gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (_settled || !IsSpawned)
        {
            return;
        }

        SandPileController pileController = SandPileController.Instance;
        if (pileController.ContainsSand(transform.position))
        {
            SettleSand(transform.position, pileController);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_settled ||
            collision.collider.GetComponent<SandParticle>() != null)
        {
            return;
        }

        SandPileController pileController = SandPileController.Instance;
        Vector2 impactPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;

        if (collision.collider.GetComponent<SandPileController>() == null)
        {
            pileController.RegisterSolidSurface(collision.collider, impactPoint);
        }

        SettleSand(impactPoint, pileController);
    }

    private void SettleSand(Vector2 impactPoint, SandPileController pileController)
    {
        if (_settled)
        {
            return;
        }

        _settled = true;
        Color color = _sprite != null ? _sprite.color : Color.yellow;
        if (_settleOverride != null)
        {
            _settleOverride(this);
        }
        else
        {
            int amount = _settleAmount > 0 ? _settleAmount : Random.Range(8, 13);
            pileController.AddSandBurst(impactPoint, color, amount);
        }

        OnDespawned();
        _recycle?.Invoke(this);
    }
}
