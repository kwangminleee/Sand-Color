using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StagePopupController : MonoBehaviour
{
    private const string SelectedStageKey = "SandColor.SelectedStage";
    [Header("Stage Data")]
    [SerializeField] private StageData[] _stages = Array.Empty<StageData>();
    [Header("Stage UI")]
    [SerializeField] private TMP_Text _stageNumberText;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _rewardText;
    [SerializeField] private Image _targetImage;
    [SerializeField] private Image[] _stars = Array.Empty<Image>();
    [SerializeField] private Sprite _starOn;
    [SerializeField] private Sprite _starOff;
    [Header("Buttons")]
    [SerializeField] private Button _previousButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _closeButton;
    private int _index;

    private void OnEnable()
    {
        if (_previousButton != null) _previousButton.onClick.AddListener(Previous);
        if (_nextButton != null) _nextButton.onClick.AddListener(Next);
        if (_startButton != null) _startButton.onClick.AddListener(StartSelectedStage);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
    }
    private void OnDisable()
    {
        if (_previousButton != null) _previousButton.onClick.RemoveListener(Previous);
        if (_nextButton != null) _nextButton.onClick.RemoveListener(Next);
        if (_startButton != null) _startButton.onClick.RemoveListener(StartSelectedStage);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
    }
    public void Open()
    {
        int saved = PlayerPrefs.GetInt(SelectedStageKey, 1); _index = 0;
        for (int i=0;i<_stages.Length;i++) if (_stages[i]!=null && _stages[i].StageNumber==saved){_index=i;break;}
        gameObject.SetActive(true); Refresh();
    }
    public void Open(StageData stage)
    {
        for(int i=0;i<_stages.Length;i++) if(_stages[i]==stage){_index=i;break;}
        gameObject.SetActive(true); Refresh();
    }
    public void Close()=>gameObject.SetActive(false);
    public void Previous(){if(_stages.Length==0)return;_index=(_index-1+_stages.Length)%_stages.Length;Refresh();}
    public void Next(){if(_stages.Length==0)return;_index=(_index+1)%_stages.Length;Refresh();}
    private void Refresh()
    {
        if(_stages.Length==0||_stages[_index]==null)return; StageData stage=_stages[_index];
        if(_stageNumberText!=null)_stageNumberText.text=$"스테이지 {stage.StageNumber}";
        if(_titleText!=null)_titleText.text=stage.DisplayName;
        if(_rewardText!=null)_rewardText.text=$"보상  {stage.CoinReward:N0}";
        if(_targetImage!=null){_targetImage.sprite=stage.TargetSprite;_targetImage.preserveAspect=true;}
        int earned=Mathf.Clamp(PlayerPrefs.GetInt($"SandColor.Stage.{stage.StageNumber}.BestStars",0),0,3);
        for(int i=0;i<_stars.Length;i++)if(_stars[i]!=null)_stars[i].sprite=i<earned?_starOn:_starOff;
    }
    private void StartSelectedStage()
    {
        if(_stages.Length==0||_stages[_index]==null)return;
        PlayerPrefs.SetInt(SelectedStageKey,_stages[_index].StageNumber);PlayerPrefs.Save();GameSceneManager.Instance.LoadGame();
    }
}
