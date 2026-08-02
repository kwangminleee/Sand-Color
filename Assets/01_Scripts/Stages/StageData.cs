using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageData",
    menuName = "Sand Color/Stage Data",
    order = 0)]
public class StageData : ScriptableObject
{
    private const float MinimumRequiredFillRatio = 0.01f;
    private const float MaximumStarThreshold = 100f;

    [Header("기본 정보")]
    [SerializeField, Min(1)] private int _stageNumber = 1;
    [SerializeField] private string _displayName = string.Empty;

    [Header("목표 이미지와 마스크")]
    [SerializeField] private Sprite _targetSprite;
    [SerializeField] private Texture2D _targetTexture;
    [SerializeField] private Texture2D _templateMask;
    [SerializeField, Range(MinimumRequiredFillRatio, 1f)]
    private float _requiredFillRatio = 0.98f;

    [Header("별과 보상")]
    [SerializeField, Range(0f, MaximumStarThreshold)]
    private float _twoStarThreshold = 50f;
    [SerializeField, Range(0f, MaximumStarThreshold)]
    private float _threeStarThreshold = 80f;
    [SerializeField, Min(0)] private int _coinReward = 150;

    [Header("스테이지 모래 색상")]
    [SerializeField] private Color[] _sandPalette = Array.Empty<Color>();

    public int StageNumber => Mathf.Max(1, _stageNumber);

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
        ? $"STAGE {StageNumber:00}"
        : _displayName.Trim();

    public Sprite TargetSprite => _targetSprite;

    public Texture2D TargetTexture => _targetTexture != null
        ? _targetTexture
        : (_targetSprite != null ? _targetSprite.texture : null);

    public Texture2D TemplateMask => _templateMask;

    public float RequiredFillRatio => Mathf.Clamp(
        _requiredFillRatio,
        MinimumRequiredFillRatio,
        1f);

    public float TwoStarThreshold => Mathf.Clamp(
        _twoStarThreshold,
        0f,
        MaximumStarThreshold);

    public float ThreeStarThreshold => Mathf.Clamp(
        Mathf.Max(_threeStarThreshold, TwoStarThreshold),
        0f,
        MaximumStarThreshold);

    public int CoinReward => Mathf.Max(0, _coinReward);

    public Color[] SandPalette => _sandPalette ?? Array.Empty<Color>();

    private void OnValidate()
    {
        _stageNumber = Mathf.Max(1, _stageNumber);
        _requiredFillRatio = Mathf.Clamp(
            _requiredFillRatio,
            MinimumRequiredFillRatio,
            1f);
        _twoStarThreshold = Mathf.Clamp(
            _twoStarThreshold,
            0f,
            MaximumStarThreshold);
        _threeStarThreshold = Mathf.Clamp(
            Mathf.Max(_threeStarThreshold, _twoStarThreshold),
            0f,
            MaximumStarThreshold);
        _coinReward = Mathf.Max(0, _coinReward);
        _sandPalette = _sandPalette ?? Array.Empty<Color>();
    }
}
