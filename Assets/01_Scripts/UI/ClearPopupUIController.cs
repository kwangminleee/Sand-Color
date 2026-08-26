using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClearPopupUIController : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject _popupRoot;

    [Header("Result UI")]
    [SerializeField] private Image _targetImage;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField]
    private Transform[] _starRoots =
        new Transform[3];

    [Header("Buttons")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _returnButton;

    private StageManager _stageManager;

    private void OnEnable()
    {
        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(
                HandleRestartButton);
        }

        if (_returnButton != null)
        {
            _returnButton.onClick.AddListener(
                HandleReturnButton);
        }
    }

    private void OnDisable()
    {
        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(
                HandleRestartButton);
        }

        if (_returnButton != null)
        {
            _returnButton.onClick.RemoveListener(
                HandleReturnButton);
        }
    }

    public void Initialize(StageManager stageManager)
    {
        _stageManager = stageManager;
    }

    public void SetTarget(Sprite targetSprite)
    {
        ApplySprite(
            _targetImage,
            targetSprite);
    }

    public void Open(
        int stars,
        float similarityPercent)
    {
        SetStars(stars);
        SetTitle(similarityPercent);

        if (_popupRoot != null)
        {
            _popupRoot.SetActive(true);
            return;
        }

        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (_popupRoot != null)
        {
            _popupRoot.SetActive(false);
            return;
        }

        gameObject.SetActive(false);
    }

    private void SetStars(int stars)
    {
        int earnedStars =
            Mathf.Clamp(stars, 0, 3);

        if (_starRoots == null)
        {
            return;
        }

        for (int index = 0;
             index < _starRoots.Length;
             index++)
        {
            Transform starRoot =
                _starRoots[index];

            if (starRoot == null)
            {
                continue;
            }

            bool earned =
                index < earnedStars;

            Transform onImage =
                starRoot.Find("On");

            Transform offImage =
                starRoot.Find("Off");

            if (onImage != null)
            {
                onImage.gameObject.SetActive(
                    earned);
            }

            if (offImage != null)
            {
                offImage.gameObject.SetActive(
                    !earned);
            }
        }
    }

    private void SetTitle(float similarityPercent)
    {
        if (_titleText == null)
        {
            return;
        }

        float safeSimilarity =
            Mathf.Clamp(
                similarityPercent,
                0f,
                100f);

        _titleText.text =
            $"CLEAR!  {safeSimilarity:0}%";
    }

    private void HandleRestartButton()
    {
        if (_stageManager == null)
        {
            Debug.LogWarning(
                "ClearPopupController에 StageManager가 " +
                "초기화되지 않았습니다.",
                this);

            return;
        }

        _stageManager.RestartGame();
    }

    private void HandleReturnButton()
    {
        if (_stageManager == null)
        {
            Debug.LogWarning(
                "ClearPopupController에 StageManager가 " +
                "초기화되지 않았습니다.",
                this);

            return;
        }

        _stageManager.GoToMain();
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