using UnityEngine;
using UnityEngine.UI;

public sealed class MainUIController : MonoBehaviour
{
    [SerializeField] private Button _freeModeButton;

    private void OnEnable()
    {
        if (_freeModeButton != null)
        {
            _freeModeButton.onClick.AddListener(StartGame);
        }
    }

    private void OnDisable()
    {
        if (_freeModeButton != null)
        {
            _freeModeButton.onClick.RemoveListener(StartGame);
        }
    }

    public void StartGame() => GameSceneManager.Instance.LoadGame();
    public void GoToTitle() => GameSceneManager.Instance.LoadTitle();
    public void ExitGame() => GameSceneManager.Instance.QuitGame();
}
