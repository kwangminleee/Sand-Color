using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StageMapController : MonoBehaviour
{
    private const string HighestUnlockedStageKey = "SandColor.HighestUnlockedStage";

    [Header("Map")]
    [SerializeField] private RectTransform _mapBackground;
    [SerializeField] private RectTransform _stageRoot;
    [SerializeField] private StagePopupController _stagePopup;
    [SerializeField, Min(1000f)] private float _backgroundHeight = 1846f;
    [SerializeField, Range(1, 10)] private int _backgroundPageCount = 3;

    [Header("Stages")]
    [SerializeField] private StageData[] _stages = Array.Empty<StageData>();
    [Tooltip("길 위 원형 발판입니다. 아래쪽부터 자동으로 정렬됩니다.")]
    [SerializeField] private RectTransform[] _stagePlatforms = Array.Empty<RectTransform>();

    private ScrollRect _scrollRect;

    private void Awake()
    {
        CreateScrollMap();
        BindStages();
    }

    private void Start()
    {
        StartCoroutine(SetInitialScrollPosition());
    }

    private void CreateScrollMap()
    {
        if (_mapBackground == null || _stageRoot == null)
        {
            Debug.LogError("StageMapController의 배경과 Stage Root를 연결하세요.", this);
            return;
        }

        RectTransform owner = transform.parent as RectTransform;
        GameObject viewportObject = new GameObject(
            "StageMapViewport", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.SetParent(owner, false);
        viewport.SetSiblingIndex(1);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        Image viewportInput = viewportObject.GetComponent<Image>();
        viewportInput.color = new Color(0f, 0f, 0f, 0f);
        viewportInput.raycastTarget = true;

        GameObject contentObject = new GameObject("StageMapContent", typeof(RectTransform));
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(1f, 0f);
        content.pivot = new Vector2(0.5f, 0f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, _backgroundHeight * _backgroundPageCount);

        BuildBackgroundPages(content);

        _stageRoot.SetParent(content, false);
        _stageRoot.anchorMin = _stageRoot.anchorMax = new Vector2(0.5f, 0f);
        _stageRoot.anchoredPosition = new Vector2(17f, _backgroundHeight * 0.5f + 141f);

        _scrollRect = viewportObject.GetComponent<ScrollRect>();
        _scrollRect.viewport = viewport;
        _scrollRect.content = content;
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.inertia = true;
        _scrollRect.scrollSensitivity = 30f;
    }

    private void BuildBackgroundPages(RectTransform content)
    {
        Image sourceImage = _mapBackground.GetComponent<Image>();
        _mapBackground.SetParent(content, false);
        ConfigureBackgroundRect(_mapBackground, 0);

        for (int page = 1; page < _backgroundPageCount; page++)
        {
            GameObject copy = new GameObject($"MapBackground_{page + 1:00}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform copyRect = copy.GetComponent<RectTransform>();
            copyRect.SetParent(content, false);
            Image copyImage = copy.GetComponent<Image>();
            copyImage.sprite = sourceImage != null ? sourceImage.sprite : null;
            copyImage.color = sourceImage != null ? sourceImage.color : Color.white;
            copyImage.raycastTarget = false;
            ConfigureBackgroundRect(copyRect, page);
        }

        _mapBackground.SetAsFirstSibling();
    }

    private void ConfigureBackgroundRect(RectTransform background, int page)
    {
        background.anchorMin = new Vector2(0f, 0f);
        background.anchorMax = new Vector2(1f, 0f);
        background.pivot = new Vector2(0.5f, 0f);
        background.anchoredPosition = new Vector2(0f, _backgroundHeight * page);
        background.sizeDelta = new Vector2(0f, _backgroundHeight);
        Image image = background.GetComponent<Image>();
        if (image != null) image.raycastTarget = false;
    }

    private void BindStages()
    {
        Array.Sort(_stagePlatforms, CompareFromBottom);
        int highestUnlocked = Mathf.Max(1, PlayerPrefs.GetInt(HighestUnlockedStageKey, 1));
        int count = Mathf.Min(_stages.Length, _stagePlatforms.Length);

        for (int index = 0; index < _stagePlatforms.Length; index++)
        {
            RectTransform platform = _stagePlatforms[index];
            if (platform == null) continue;

            bool hasStage = index < count && _stages[index] != null;
            platform.gameObject.SetActive(hasStage);
            if (!hasStage) continue;

            StageMapButton button = platform.GetComponent<StageMapButton>();
            if (button == null) button = platform.gameObject.AddComponent<StageMapButton>();

            StageData stage = _stages[index];
            button.Bind(stage, stage.StageNumber <= highestUnlocked, OpenStagePopup);
            platform.name = $"Stage_{stage.StageNumber:00}_{stage.DisplayName}";
        }
    }

    private void OpenStagePopup(StageData stage)
    {
        if (_stagePopup == null)
        {
            Debug.LogError("StageMapController에 StagePopupController를 연결하세요.", this);
            return;
        }

        _stagePopup.Open(stage);
    }

    private IEnumerator SetInitialScrollPosition()
    {
        yield return null;
        if (_scrollRect == null) yield break;
        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
        _scrollRect.StopMovement();
    }

    private static int CompareFromBottom(RectTransform left, RectTransform right)
    {
        if (left == null) return 1;
        if (right == null) return -1;
        return left.anchoredPosition.y.CompareTo(right.anchoredPosition.y);
    }
}
