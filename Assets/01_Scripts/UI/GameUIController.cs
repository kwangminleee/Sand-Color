using UnityEngine;
using UnityEngine.UI;

public sealed class GameUIController : MonoBehaviour
{
    [SerializeField] private Button _pauseButton;

    private void OnEnable()
    {
        _pauseButton.onClick.AddListener(GoToMain);
    }

    private void OnDisable()
    {
        _pauseButton.onClick.RemoveListener(GoToMain);
    }

    public void GoToMain()
    {
        GameSceneManager.Instance.LoadMain();
    }

    public void GoToTitle()
    {
        GameSceneManager.Instance.LoadTitle();
    }

    public void RestartGame()
    {
        GameSceneManager.Instance.ReloadCurrentScene();
    }
}
