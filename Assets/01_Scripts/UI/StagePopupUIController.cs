using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StagePopupUIController : MonoBehaviour
{
    private StageData[] _stages = Array.Empty<StageData>();
    private GameObject _stagePopup;
    private TMP_Text _stageNumberText;
    private Image _referenceImage;
    private Transform _previous, _next, _freeModeBackground, _collectionBackground;
    private MainUIController _host;
    private int _index;

    public void SetHost(MainUIController host) => _host = host;

    private void Awake()
    {
        _stagePopup = FindObjectInControllerScope("StagePopup");
        _stages = Resources.LoadAll<StageData>("Stages").Where(x => x != null).OrderBy(x => x.StageNumber).ToArray();
        SaveManager.Initialize(_stages);
        _stageNumberText = FindComponent<TMP_Text>("StageNumTitleTxt");
        _referenceImage = FindImageBelow("Target", "BG");
        _previous = FindTransform("PreviousBtn");
        _next = FindTransform("NextBtn");
        _freeModeBackground = FindBelow(FindTransform("FreeModeBtn"), "BG");
        _collectionBackground = FindBelow(FindTransform("CollectionBtn"), "BG");
    }

    public void Open()
    {
        int selected = SaveManager.Data.selectedStageId;
        int found = Array.FindIndex(_stages, x => x.Id == selected);
        _index = found < 0 ? 0 : found;
        if (_stagePopup != null) _stagePopup.SetActive(true);
        SetSelectedTab(false, false);
        Refresh();
    }

    public void Close() { if (_stagePopup != null) _stagePopup.SetActive(false); }
    public void Previous() { if (_stages.Length == 0) return; _index = (_index - 1 + _stages.Length) % _stages.Length; Refresh(); }
    public void Next() { if (_stages.Length == 0) return; _index = (_index + 1) % _stages.Length; Refresh(); }

    public void SelectInfiniteMode()
    {
        ResolveHost();
        if (_host != null) _host.OpenFreeModePopup();
        SetSelectedTab(true, false);
    }

    public void SelectCollection()
    {
        ResolveHost();
        if (_host != null) _host.OpenCollection();
        SetSelectedTab(false, true);
    }

    public void ClearTabSelection() => SetSelectedTab(false, false);

    private void ResolveHost()
    {
        if (_host == null && transform.parent != null)
            _host = transform.parent.GetComponentInChildren<MainUIController>(true);
    }

    public void StartSelectedStage()
    {
        if (_stages.Length == 0) return;
        SaveManager.SelectStage(_stages[_index]);
        GameSceneManager.Instance.LoadGame();
    }

    private void Refresh()
    {
        if (_stageNumberText != null)
            _stageNumberText.text = _stages.Length > 0 ? $"스테이지 {_stages[_index].StageNumber}" : "스테이지 1";
        if (_previous != null) _previous.gameObject.SetActive(true);
        if (_next != null) _next.gameObject.SetActive(true);
        if (_referenceImage != null)
        {
            _referenceImage.gameObject.SetActive(_stages.Length > 0);
            if (_stages.Length > 0)
            {
                _referenceImage.sprite = _stages[_index].TargetSprite;
                _referenceImage.preserveAspect = true;
            }
        }
        StageProgressData progress = _stages.Length == 0
            ? null
            : SaveManager.GetStageProgress(_stages[_index]);
        SetStars(progress == null ? 0 : Mathf.Clamp(progress.bestStars, 0, 3));
    }

    private void SetSelectedTab(bool freeMode, bool collection)
    {
        if (_freeModeBackground != null) _freeModeBackground.gameObject.SetActive(freeMode);
        if (_collectionBackground != null) _collectionBackground.gameObject.SetActive(collection);
    }

    private void SetStars(int earned)
    {
        for (int number = 1; number <= 3; number++)
        {
            Transform star = FindTransform(number.ToString());
            if (star == null) continue;
            Transform on = FindBelow(star, "On"), off = FindBelow(star, "Off");
            if (on != null) on.gameObject.SetActive(number <= earned);
            if (off != null) off.gameObject.SetActive(number > earned);
        }
    }

    private Image FindImageBelow(string parentName, string childName)
    {
        Transform child = FindBelow(FindTransform(parentName), childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private T FindComponent<T>(string name) where T : Component
    {
        Transform item = FindTransform(name); return item != null ? item.GetComponent<T>() : null;
    }

    private Transform FindTransform(string name)
    {
        if (_stagePopup == null) return null;
        foreach (Transform item in _stagePopup.GetComponentsInChildren<Transform>(true)) if (item.name == name) return item;
        return null;
    }

    private GameObject FindObjectInControllerScope(string name)
    {
        Transform scope = transform.parent != null ? transform.parent : transform;
        foreach (Transform item in scope.GetComponentsInChildren<Transform>(true))
            if (item.name == name) return item.gameObject;
        return null;
    }

    private static Transform FindBelow(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true)) if (item.name == name) return item;
        return null;
    }
}
