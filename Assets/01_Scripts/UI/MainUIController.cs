using UnityEngine;
using UnityEngine.UI;

public sealed class MainUIController : MonoBehaviour
{
    [SerializeField] private Button _freeModeButton;

    private void OnEnable()
    {
        _freeModeButton.onClick.AddListener(StartGame);
    }

    private void OnDisable()
    {
        _freeModeButton.onClick.RemoveListener(StartGame);
    }

    public void StartGame()
    {
        GameSceneManager.Instance.LoadGame();
    }

    public void GoToTitle()
    {
        GameSceneManager.Instance.LoadTitle();
    }

    public void ExitGame()
    {
        GameSceneManager.Instance.QuitGame();
    }
}
