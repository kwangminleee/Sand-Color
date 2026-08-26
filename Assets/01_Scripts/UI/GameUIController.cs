using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _skipButton;

    [Header("Color UI")]
    [SerializeField] private Image _currentColorImage;
    [SerializeField] private Image _nextColorImage;

    [Header("Target UI")]
    [SerializeField] private Image _targetImage;

    [Header("Progress UI")]
    [SerializeField] private RectTransform _progressFill;
    [SerializeField] private TMP_Text _progressText;

    private StageManager _stageManager;
    private float _progressFullAnchorMaxX = 1f;

    private void Awake()
    {
        if (_progressFill != null)
        {
            _progressFullAnchorMaxX =
                _progressFill.anchorMax.x;
        }

        SetProgress(0f);
    }

    private void OnEnable()
    {
        if (_pauseButton != null)
        {
            _pauseButton.onClick.AddListener(
                HandlePauseButton);
        }

        if (_skipButton != null)
        {
            _skipButton.onClick.AddListener(
                HandleSkipButton);
        }
    }

    private void OnDisable()
    {
        if (_pauseButton != null)
        {
            _pauseButton.onClick.RemoveListener(
                HandlePauseButton);
        }

        if (_skipButton != null)
        {
            _skipButton.onClick.RemoveListener(
                HandleSkipButton);
        }
    }

    public void Initialize(StageManager stageManager)
    {
        _stageManager = stageManager;
    }

    public void SetColors(
        Color currentColor,
        Color nextColor)
    {
        if (_currentColorImage != null)
        {
            _currentColorImage.color =
                currentColor;
        }

        if (_nextColorImage != null)
        {
            _nextColorImage.color =
                nextColor;
        }
    }

    public void SetTarget(Sprite targetSprite)
    {
        ApplySprite(
            _targetImage,
            targetSprite);
    }

    public void SetInfiniteMode(bool infiniteMode)
    {
        if (_targetImage != null) _targetImage.gameObject.SetActive(!infiniteMode);
        if (_progressFill != null && _progressFill.parent != null)
            _progressFill.parent.gameObject.SetActive(!infiniteMode);
    }

    public void SetProgress(float normalizedProgress)
    {
        float progress =
            Mathf.Clamp01(normalizedProgress);

        if (_progressFill != null)
        {
            Vector2 anchorMax =
                _progressFill.anchorMax;

            anchorMax.x = Mathf.Lerp(
                _progressFill.anchorMin.x,
                _progressFullAnchorMaxX,
                progress);

            _progressFill.anchorMax =
                anchorMax;
        }

        if (_progressText != null)
        {
            _progressText.text =
                $"OUTLINE  {progress * 100f:0}%";
        }
    }

    private void HandlePauseButton()
    {
        if (_stageManager == null)
        {
            Debug.LogWarning(
                "GameUIController의 StageManager가 초기화되지 않았습니다.",
                this);

            return;
        }

        _stageManager.GoToMain();
    }

    private void HandleSkipButton()
    {
        if (_stageManager == null)
        {
            Debug.LogWarning(
                "GameUIController의 StageManager가 초기화되지 않았습니다.",
                this);

            return;
        }

        _stageManager.SkipColor();
    }

    private static void ApplySprite(
        Image image,
        Sprite sprite)
    {
        if (image == null || sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
    }
}
