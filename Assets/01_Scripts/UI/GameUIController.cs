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

    private GameObject _pausePopup;
    private GameObject _settingPopup;
    private Button _pauseCloseButton;
    private Button _pauseConfirmButton;
    private Button _resumeContentButton;
    private Button _retryContentButton;
    private Button _settingsContentButton;
    private Button _homeContentButton;
    private Button _settingCloseButton;
    private Button _settingConfirmButton;

    private StageManager _stageManager;
    private void Awake()
    {
        ResolvePopupReferences();
        SetPopupActive(_pausePopup, false);
        SetPopupActive(_settingPopup, false);
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

        AddListener(_pauseCloseButton, HandleResumeButton);
        AddListener(_pauseConfirmButton, HandlePauseConfirmButton);
        AddListener(_resumeContentButton, HandleResumeButton);
        AddListener(_retryContentButton, HandleRetryButton);
        AddListener(_settingsContentButton, HandleOpenSettingsButton);
        AddListener(_homeContentButton, HandleHomeButton);
        AddListener(_settingCloseButton, HandleCloseSettingsButton);
        AddListener(_settingConfirmButton, HandleCloseSettingsButton);
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

        RemoveListener(_pauseCloseButton, HandleResumeButton);
        RemoveListener(_pauseConfirmButton, HandlePauseConfirmButton);
        RemoveListener(_resumeContentButton, HandleResumeButton);
        RemoveListener(_retryContentButton, HandleRetryButton);
        RemoveListener(_settingsContentButton, HandleOpenSettingsButton);
        RemoveListener(_homeContentButton, HandleHomeButton);
        RemoveListener(_settingCloseButton, HandleCloseSettingsButton);
        RemoveListener(_settingConfirmButton, HandleCloseSettingsButton);
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
        Transform targetArt = FindAncestor(
            _targetImage != null ? _targetImage.transform : null,
            "TargetArt");

        if (targetArt == null)
        {
            targetArt = FindDescendant(transform.root, "TargetArt");
        }

        if (targetArt != null)
        {
            targetArt.gameObject.SetActive(!infiniteMode);
        }

        Transform sandFillButton = FindDescendant(transform.root, "SandFillBtn");
        if (sandFillButton != null) sandFillButton.gameObject.SetActive(false);
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

        Time.timeScale = 0f;
        SetPopupActive(_pausePopup, true);
    }

    private void HandleResumeButton()
    {
        SetPopupActive(_pausePopup, false);
        Time.timeScale = 1f;
    }

    private void HandlePauseConfirmButton()
    {
        Time.timeScale = 1f;
        _stageManager?.GoToMain();
    }

    private void HandleRetryButton()
    {
        Time.timeScale = 1f;
        _stageManager?.RestartGame();
    }

    private void HandleOpenSettingsButton()
    {
        SetPopupActive(_settingPopup, true);
    }

    private void HandleHomeButton()
    {
        Time.timeScale = 1f;
        _stageManager?.GoToMain();
    }

    private void HandleCloseSettingsButton()
    {
        SetPopupActive(_settingPopup, false);
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

    private void ResolvePopupReferences()
    {
        Transform root = transform.root;
        _pausePopup = FindDescendant(root, "PausePopup")?.gameObject;
        _settingPopup = FindDescendant(root, "SettingPopup")?.gameObject;

        _pauseCloseButton = FindDescendant(
            _pausePopup != null ? _pausePopup.transform : null,
            "CloseBtn")?.GetComponent<Button>();
        _pauseConfirmButton = FindDescendant(
            _pausePopup != null ? _pausePopup.transform : null,
            "ConfirmBtn")?.GetComponent<Button>();

        Transform pauseContent = FindDescendant(
            _pausePopup != null ? _pausePopup.transform : null,
            "PauseContent");
        Transform pauseMenuRoot = pauseContent != null
            ? pauseContent
            : (_pausePopup != null ? _pausePopup.transform : null);

        _resumeContentButton = ConfigureContentButton(
            FindDescendant(pauseMenuRoot, "ResumeContent")?.gameObject);
        _retryContentButton = ConfigureContentButton(
            FindDescendant(pauseMenuRoot, "RetryContent")?.gameObject);
        _settingsContentButton = ConfigureContentButton(
            FindDescendant(pauseMenuRoot, "SettingsContent")?.gameObject);
        _homeContentButton = ConfigureContentButton(
            FindDescendant(pauseMenuRoot, "HomeContent")?.gameObject);

        _settingCloseButton = FindDescendant(
            _settingPopup != null ? _settingPopup.transform : null,
            "CloseBtn")?.GetComponent<Button>();
        _settingConfirmButton = FindDescendant(
            _settingPopup != null ? _settingPopup.transform : null,
            "ConfirmBtn")?.GetComponent<Button>();
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null) return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName) return child;
        }

        return null;
    }

    private static Transform FindAncestor(Transform start, string objectName)
    {
        Transform current = start;
        while (current != null)
        {
            if (current.name == objectName) return current;
            current = current.parent;
        }

        return null;
    }

    private static void SetPopupActive(GameObject popup, bool active)
    {
        if (popup != null) popup.SetActive(active);
    }

    private static Button ConfigureContentButton(GameObject target)
    {
        if (target == null) return null;

        Image hitArea = target.GetComponent<Image>();
        if (hitArea == null)
        {
            hitArea = target.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);
        }

        hitArea.raycastTarget = true;

        foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic != hitArea) graphic.raycastTarget = false;
        }

        Button button = target.GetComponent<Button>() ?? target.AddComponent<Button>();
        button.targetGraphic = hitArea;
        return button;
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.RemoveListener(action);
    }
}
