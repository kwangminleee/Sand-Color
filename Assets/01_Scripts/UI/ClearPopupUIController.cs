using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ClearPopupUIController : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject _popupRoot;

    [Header("Clear Result UI")]
    [SerializeField] private Image _targetImage;
    [SerializeField] private TMP_Text _similarityText;
    [SerializeField] private GameObject[] _starOnObjects = new GameObject[3];
    [SerializeField] private GameObject[] _starOffObjects = new GameObject[3];

    [Header("Buttons")]
    [SerializeField] private Button _homeButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _nextStageButton;

    private StageManager _stageManager;

    private void Awake() => ResolveReferences();

    private void OnEnable()
    {
        ResolveReferences();
        AddListener(_homeButton, HandleReturnButton);
        AddListener(_retryButton, HandleRestartButton);
        AddListener(_nextStageButton, HandleNextStageButton);
    }

    private void OnDisable()
    {
        RemoveListener(_homeButton, HandleReturnButton);
        RemoveListener(_retryButton, HandleRestartButton);
        RemoveListener(_nextStageButton, HandleNextStageButton);
    }

    public void Initialize(StageManager stageManager)
    {
        _stageManager = stageManager;
        ResolveReferences();
    }

    public void SetTarget(Sprite targetSprite)
    {
        ApplySprite(_targetImage, targetSprite);
    }

    public void Open(int stars, float similarityPercent)
    {
        ResolveReferences();
        SetStars(stars);
        SetSimilarity(similarityPercent);
        SetActive(_popupRoot, true);
    }

    public void Close()
    {
        SetActive(_popupRoot, false);
    }

    private void SetStars(int stars)
    {
        int earnedStars = Mathf.Clamp(stars, 0, 3);
        if (_starOnObjects == null || _starOffObjects == null) return;

        for (int index = 0; index < 3; index++)
        {
            bool earned = index < earnedStars;
            if (index < _starOnObjects.Length) SetActive(_starOnObjects[index], earned);
            if (index < _starOffObjects.Length) SetActive(_starOffObjects[index], !earned);
        }
    }

    private void SetSimilarity(float similarityPercent)
    {
        if (_similarityText != null)
        {
            _similarityText.text = $"유사도  {Mathf.Clamp(similarityPercent, 0f, 100f):0}%";
        }
    }

    private void HandleRestartButton()
    {
        if (_stageManager == null)
        {
            Debug.LogWarning("결과 팝업의 StageManager가 초기화되지 않았습니다.", this);
            return;
        }

        _stageManager.RestartGame();
    }

    private void HandleReturnButton()
    {
        if (_stageManager == null)
        {
            Debug.LogWarning("결과 팝업의 StageManager가 초기화되지 않았습니다.", this);
            return;
        }

        _stageManager.GoToMain();
    }

    private void HandleNextStageButton()
    {
        if (_stageManager == null)
        {
            Debug.LogWarning("결과 팝업의 StageManager가 초기화되지 않았습니다.", this);
            return;
        }

        _stageManager.GoToNextStage();
    }

    private void ResolveReferences()
    {
        Transform searchRoot = transform.root;
        _popupRoot = _popupRoot != null
            ? _popupRoot
            : FindDescendant(searchRoot, "GameClearPopup")?.gameObject;
        ResolveClearPopupReferences();
    }

    private void ResolveClearPopupReferences()
    {
        if (_popupRoot == null) return;
        Transform root = _popupRoot.transform;
        _targetImage ??= FindDescendant(root, "TargetArt")?.GetComponent<Image>();
        _similarityText ??= FindDescendant(root, "SimilarityText")?.GetComponent<TMP_Text>();
        _homeButton ??= FindDescendant(root, "HomeButton")?.GetComponent<Button>();
        _retryButton ??= FindDescendant(root, "RetryButton")?.GetComponent<Button>();
        _nextStageButton ??= FindDescendant(root, "NextStageButton")?.GetComponent<Button>();

        if (_starOnObjects == null || _starOnObjects.Length != 3) _starOnObjects = new GameObject[3];
        if (_starOffObjects == null || _starOffObjects.Length != 3) _starOffObjects = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            Transform star = FindDescendant(root, $"Star{i + 1}");
            if (star == null) continue;
            _starOnObjects[i] ??= star.Find("On")?.gameObject;
            _starOffObjects[i] ??= star.Find("Off")?.gameObject;
        }
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

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }

    private static void AddListener(Button button, UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    private static void RemoveListener(Button button, UnityAction action)
    {
        if (button != null) button.onClick.RemoveListener(action);
    }

    private static void ApplySprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null) return;

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
    }
}
