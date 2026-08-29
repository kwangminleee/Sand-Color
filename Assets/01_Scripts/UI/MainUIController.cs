using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainUIController : MonoBehaviour
{
    private const string SelectedStageKey = "SandColor.SelectedStage";
    private const string InfiniteModeKey = "SandColor.InfiniteMode";
    private Transform _root;
    private StagePopupUIController _stagePopup;
    private GameObject _collectionPopup, _freeModePopup, _settingPopup, _shopPopup;
    private GameObject _goldContent, _heartContent, _goldTabBackground, _heartTabBackground;

    private void Awake()
    {
        _root = transform.parent != null ? transform.parent : transform;
        GameObject stage = FindObject("StagePopup");
        _collectionPopup = FindObject("CollectionPopup");
        _freeModePopup = FindObject("FreeModePopup");
        _settingPopup = FindObject("SettingPopup");
        _shopPopup = FindObject("ShopPopup");
        _goldContent = FindObjectIn(_shopPopup, "GoldContent");
        _heartContent = FindObjectIn(_shopPopup, "HeartContent");
        _goldTabBackground = FindObjectIn(FindObjectIn(_shopPopup, "GoldTabButton"), "BG");
        _heartTabBackground = FindObjectIn(FindObjectIn(_shopPopup, "HeartTabButton"), "BG");

        StagePopupUIController duplicatedFreeModeController =
            _freeModePopup != null ? _freeModePopup.GetComponent<StagePopupUIController>() : null;
        if (duplicatedFreeModeController != null) Destroy(duplicatedFreeModeController);

        ConfigureTabHitArea(FindObjectIn(stage, "FreeModeBtn"));
        ConfigureTabHitArea(FindObjectIn(stage, "CollectionBtn"));
        ConfigureTabHitArea(FindObjectIn(_shopPopup, "GoldTabButton"));
        ConfigureTabHitArea(FindObjectIn(_shopPopup, "HeartTabButton"));
        ConfigureTabHitArea(FindObjectIn(_freeModePopup, "FreeModeBtn"));
        ConfigureTabHitArea(FindObjectIn(_freeModePopup, "CollectionBtn"));
        ConfigureRuntimeButton(FindObjectIn(_freeModePopup, "CloseBtn"), CloseFreeModePopup, true);
        ConfigureRuntimeButton(FindObjectIn(_freeModePopup, "CollectionBtn"), SelectCollectionFromFreeModePopup, true);
        ConfigureRuntimeButton(FindObjectIn(_freeModePopup, "StartBtn"), StartInfiniteModeGame, true);

        _stagePopup = _root.GetComponentInChildren<StagePopupUIController>(true);
        if (_stagePopup != null) _stagePopup.SetHost(this);
        CloseAllPopups();
    }

    public void OpenStages()
    {
        CloseAllPopups();
        if (_stagePopup != null) _stagePopup.Open();
    }

    public void OpenCollection()
    {
        CloseFreeModePopup();
        if (_settingPopup != null) _settingPopup.SetActive(false);
        if (_shopPopup != null) _shopPopup.SetActive(false);
        if (_collectionPopup != null) _collectionPopup.SetActive(true);
    }

    public void OpenFreeModePopup()
    {
        CloseCollection();
        if (_freeModePopup == null) return;
        _freeModePopup.SetActive(true);
        SetObjectActive(FindObjectIn(_freeModePopup, "PreviousBtn"), false);
        SetObjectActive(FindObjectIn(_freeModePopup, "NextBtn"), false);

        TMP_Text title = FindObjectIn(_freeModePopup, "TitleTxt")?.GetComponent<TMP_Text>();
        if (title != null) title.text = "무한모드";

        SetObjectActive(FindObjectIn(FindObjectIn(_freeModePopup, "FreeModeBtn"), "BG"), true);
        SetObjectActive(FindObjectIn(FindObjectIn(_freeModePopup, "CollectionBtn"), "BG"), false);
    }

    public void SelectCollectionFromFreeModePopup()
    {
        CloseFreeModePopup();
        if (_stagePopup != null) _stagePopup.SelectCollection();
    }

    public void StartInfiniteModeGame()
    {
        PlayerPrefs.SetInt(SelectedStageKey, 0);
        PlayerPrefs.SetInt(InfiniteModeKey, 1);
        PlayerPrefs.Save();
        GameSceneManager.Instance.LoadGame();
    }

    public void OpenSettings()
    {
        CloseAllPopups();
        if (_settingPopup != null) _settingPopup.SetActive(true);
    }

    public void OpenGoldShop() => OpenShop(true);
    public void OpenHeartShop() => OpenShop(false);
    public void SelectGoldShopTab() => ShowShopContent(true);
    public void SelectHeartShopTab() => ShowShopContent(false);
    public void CloseCollection()
    {
        if (_collectionPopup != null) _collectionPopup.SetActive(false);
        if (_stagePopup != null) _stagePopup.ClearTabSelection();
    }

    public void CloseFreeModePopup()
    {
        if (_freeModePopup != null) _freeModePopup.SetActive(false);
        SetObjectActive(FindObjectIn(FindObjectIn(_freeModePopup, "FreeModeBtn"), "BG"), false);
        SetObjectActive(FindObjectIn(FindObjectIn(_freeModePopup, "CollectionBtn"), "BG"), false);
        if (_stagePopup != null) _stagePopup.ClearTabSelection();
    }
    public void CloseSettings() { if (_settingPopup != null) _settingPopup.SetActive(false); }
    public void CloseShop() { if (_shopPopup != null) _shopPopup.SetActive(false); }

    private void OpenShop(bool showGold)
    {
        CloseAllPopups();
        if (_shopPopup != null) _shopPopup.SetActive(true);
        ShowShopContent(showGold);
    }

    private void ShowShopContent(bool showGold)
    {
        if (_goldContent != null) _goldContent.SetActive(showGold);
        if (_heartContent != null) _heartContent.SetActive(!showGold);
        if (_goldTabBackground != null) _goldTabBackground.SetActive(showGold);
        if (_heartTabBackground != null) _heartTabBackground.SetActive(!showGold);
    }

    private void CloseAllPopups()
    {
        if (_stagePopup != null) _stagePopup.Close();
        CloseCollection(); CloseFreeModePopup(); CloseSettings(); CloseShop();
    }

    private GameObject FindObject(string name) => FindObjectIn(_root.gameObject, name);

    private static GameObject FindObjectIn(GameObject root, string name)
    {
        if (root == null) return null;
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            if (item.name == name) return item.gameObject;
        return null;
    }

    private static void SetObjectActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }

    private static void ConfigureTabHitArea(GameObject tab)
    {
        if (tab == null) return;
        Image hitArea = tab.GetComponent<Image>() ?? tab.AddComponent<Image>();
        hitArea.sprite = null;
        hitArea.color = new Color(1f, 1f, 1f, 0f);
        hitArea.raycastTarget = true;

        foreach (Graphic graphic in tab.GetComponentsInChildren<Graphic>(true))
            if (graphic != hitArea) graphic.raycastTarget = false;

        Button button = tab.GetComponent<Button>();
        if (button != null) button.targetGraphic = hitArea;
    }

    private static void ConfigureRuntimeButton(
        GameObject target,
        UnityEngine.Events.UnityAction action,
        bool replaceExisting = false)
    {
        if (target == null) return;
        Button button = target.GetComponent<Button>() ?? target.AddComponent<Button>();
        Graphic graphic = target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
        if (graphic != null)
        {
            graphic.raycastTarget = true;
            button.targetGraphic = graphic;
        }
        if (replaceExisting)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }
        else if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(action);
        }
    }

    public void StartGame() => StartInfiniteModeGame();
    public void GoToTitle() => GameSceneManager.Instance.LoadTitle();
    public void ExitGame() => GameSceneManager.Instance.QuitGame();
}
