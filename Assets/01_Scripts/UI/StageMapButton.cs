using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StageMapButton : MonoBehaviour
{
    private const string StarsKeyFormat = "SandColor.Stage.{0}.BestStars";
    private static readonly Color UnlockedColor = new Color(0.48f, 0.18f, 0.95f, 1f);
    private static readonly Color LockedColor = new Color(0.38f, 0.38f, 0.42f, 0.82f);

    private Button _button;
    private Image _platformImage;
    private TMP_Text _stageNumberText;
    private TMP_Text _starsText;
    private StageData _stage;
    private Action<StageData> _onSelected;

    public void Bind(StageData stage, bool unlocked, Action<StageData> onSelected)
    {
        _stage = stage;
        _onSelected = onSelected;
        EnsureView();

        _button.interactable = unlocked;
        _platformImage.color = unlocked ? UnlockedColor : LockedColor;
        _stageNumberText.text = unlocked ? stage.StageNumber.ToString() : "LOCK";

        int stars = Mathf.Clamp(PlayerPrefs.GetInt(
            string.Format(StarsKeyFormat, stage.StageNumber), 0), 0, 3);
        _starsText.text = unlocked
            ? $"STARS {stars}/3"
            : string.Empty;
    }

    private void EnsureView()
    {
        _platformImage = GetComponent<Image>();
        _button = GetComponent<Button>();
        if (_button == null)
        {
            _button = gameObject.AddComponent<Button>();
        }

        _button.targetGraphic = _platformImage;
        _button.onClick.RemoveListener(SelectStage);
        _button.onClick.AddListener(SelectStage);

        _stageNumberText = FindOrCreateText("StageNumber", new Vector2(0f, 7f), 34f);
        _starsText = FindOrCreateText("Stars", new Vector2(0f, -54f), 22f);
    }

    private TMP_Text FindOrCreateText(string objectName, Vector2 position, float fontSize)
    {
        Transform child = transform.Find(objectName);
        TMP_Text text;
        if (child != null && child.TryGetComponent(out text))
        {
            return text;
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(160f, 48f);

        text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void SelectStage()
    {
        _onSelected?.Invoke(_stage);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(SelectStage);
        }
    }
}
