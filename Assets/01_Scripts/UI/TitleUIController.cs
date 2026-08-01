using UnityEngine;
using UnityEngine.UI;

public sealed class TitleUIController : MonoBehaviour
{
    [SerializeField] private Button _screenButton;

    private bool _isMoving;

    private void OnEnable()
    {
        _screenButton.onClick.AddListener(GoToMain);
    }

    private void OnDisable()
    {
        _screenButton.onClick.RemoveListener(GoToMain);
    }

    public void GoToMain()
    {
        if (_isMoving)
        {
            return;
        }

        _isMoving = true;
        GameSceneManager.Instance.LoadMain();
    }
}
