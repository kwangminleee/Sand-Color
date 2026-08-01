using UnityEngine;
using UnityEngine.UI;

public sealed class GameUIController : MonoBehaviour
{
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Image _currentColorImage;
    [SerializeField] private Image _nextColorImage;

    private SandSpawner _sandSpawner;

    private void Awake()
    {
        _sandSpawner = FindObjectOfType<SandSpawner>();
        BindColorUiIfNeeded();
    }

    private void OnEnable()
    {
        if (_pauseButton != null)
        {
            _pauseButton.onClick.AddListener(GoToMain);
        }

        if (_skipButton != null)
        {
            _skipButton.onClick.AddListener(SkipColor);
        }

        if (_sandSpawner != null)
        {
            _sandSpawner.ColorsChanged += RefreshColors;
            RefreshColors(
                _sandSpawner.CurrentColor,
                _sandSpawner.NextColor,
                _sandSpawner.RemainingParticles
            );
        }
    }

    private void OnDisable()
    {
        if (_pauseButton != null)
        {
            _pauseButton.onClick.RemoveListener(GoToMain);
        }

        if (_skipButton != null)
        {
            _skipButton.onClick.RemoveListener(SkipColor);
        }

        if (_sandSpawner != null)
        {
            _sandSpawner.ColorsChanged -= RefreshColors;
        }
    }

    public void SkipColor()
    {
        _sandSpawner?.SkipColor();
    }

    private void RefreshColors(Color current, Color next, int remaining)
    {
        if (_currentColorImage != null)
        {
            _currentColorImage.color = current;
        }

        if (_nextColorImage != null)
        {
            _nextColorImage.color = next;
        }
    }

    private void BindColorUiIfNeeded()
    {
        Transform root = transform.root;
        Transform skip = FindChild(root, "SkipBtn");
        if (_skipButton == null && skip != null)
        {
            _skipButton = skip.GetComponent<Button>();
            if (_skipButton == null)
            {
                _skipButton = skip.gameObject.AddComponent<Button>();
            }

            _skipButton.targetGraphic = skip.GetComponentInChildren<Image>(true);
        }

        if (_currentColorImage == null)
        {
            Transform current = FindChild(root, "CurrentColor");
            Transform swatch = current != null ? FindChild(current, "Sprite") : null;
            _currentColorImage = swatch != null ? swatch.GetComponent<Image>() : null;
        }

        if (_nextColorImage == null)
        {
            Transform next = FindChild(root, "NextColor");
            Transform swatch = next != null ? FindChild(next, "Sprite") : null;
            _nextColorImage = swatch != null ? swatch.GetComponent<Image>() : null;
        }
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform result = FindChild(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
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
