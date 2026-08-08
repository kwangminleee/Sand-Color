using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StagePopupController : MonoBehaviour
{
    private const string SelectedStageKey = "SandColor.SelectedStage";
    private const string StarsKeyFormat = "SandColor.Stage.{0}.BestStars";

    private StageData _selectedStage;
    private Button _startButton;
    private Button _closeButton;
    private TMP_Text _stageNumberText;
    private TMP_Text _titleText;
    private Image _targetImage;
    private Transform[] _starRoots;

    public void Open(StageData stage)
    {
        if (stage == null) return;
        _selectedStage = stage;
        gameObject.SetActive(true);
        EnsureReferences();

        if (_stageNumberText != null) _stageNumberText.text = $"STAGE {stage.StageNumber:00}";
        if (_titleText != null) _titleText.text = stage.DisplayName;
        if (_targetImage != null && stage.TargetSprite != null)
        {
            _targetImage.sprite = stage.TargetSprite;
            _targetImage.preserveAspect = true;
            _targetImage.color = Color.white;
        }

        SetStars(PlayerPrefs.GetInt(string.Format(StarsKeyFormat, stage.StageNumber), 0));
    }

    public void Close() => gameObject.SetActive(false);

    private void EnsureReferences()
    {
        if (_startButton != null) return;

        Transform start = FindDeep(transform, "StartBtn");
        Transform close = FindDeep(transform, "ExitBtn");
        _stageNumberText = FindComponentDeep<TMP_Text>(transform, "StageNumTxt");
        _titleText = FindComponentDeep<TMP_Text>(transform, "TitleTxt");
        _targetImage = FindComponentDeep<Image>(transform, "Target");

        _startButton = EnsureButton(start);
        _closeButton = EnsureButton(close);
        if (_startButton != null) _startButton.onClick.AddListener(StartSelectedStage);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);

        Transform star = FindDeep(transform, "Star");
        _starRoots = star != null ? new Transform[star.childCount] : new Transform[0];
        for (int i = 0; i < _starRoots.Length; i++) _starRoots[i] = star.GetChild(i);
    }

    private void StartSelectedStage()
    {
        if (_selectedStage == null) return;
        PlayerPrefs.SetInt(SelectedStageKey, _selectedStage.StageNumber);
        PlayerPrefs.Save();
        GameSceneManager.Instance.LoadGame();
    }

    private void SetStars(int stars)
    {
        stars = Mathf.Clamp(stars, 0, 3);
        if (_starRoots == null) return;
        for (int i = 0; i < _starRoots.Length; i++)
        {
            Transform on = _starRoots[i].Find("On");
            Transform off = _starRoots[i].Find("Off");
            if (on != null) on.gameObject.SetActive(i < stars);
            if (off != null) off.gameObject.SetActive(i >= stars);
        }
    }

    private static Button EnsureButton(Transform target)
    {
        if (target == null) return null;
        Button button = target.GetComponent<Button>();
        if (button == null) button = target.gameObject.AddComponent<Button>();
        if (button.targetGraphic == null) button.targetGraphic = target.GetComponent<Graphic>();
        return button;
    }

    private static T FindComponentDeep<T>(Transform root, string objectName) where T : Component
    {
        Transform target = FindDeep(root, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindDeep(Transform root, string objectName)
    {
        if (root.name == objectName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), objectName);
            if (found != null) return found;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (_startButton != null) _startButton.onClick.RemoveListener(StartSelectedStage);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
    }
}
