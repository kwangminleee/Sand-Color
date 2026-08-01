using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameScene
{
    Title = 0,
    Main = 1,
    Game = 2
}

public sealed class GameSceneManager : MonoSingleton<GameSceneManager>
{
    private const string TitleSceneName = "00_Title";
    private const string MainSceneName = "01_Main";
    private const string GameSceneName = "02_Game";

    private bool _isLoading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        _ = Instance;
    }

    protected override void Awake()
    {
        isDonDestroy = true;
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void LoadScene(GameScene scene)
    {
        LoadScene(GetSceneName(scene));
    }

    public void LoadTitle() => LoadScene(GameScene.Title);
    public void LoadMain() => LoadScene(GameScene.Main);
    public void LoadGame() => LoadScene(GameScene.Game);

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name, true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadScene(string sceneName)
    {
        LoadScene(sceneName, false);
    }

    private void LoadScene(string sceneName, bool allowReload)
    {
        if (_isLoading || (!allowReload && SceneManager.GetActiveScene().name == sceneName))
        {
            return;
        }

        _isLoading = true;
        SceneManager.LoadSceneAsync(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isLoading = false;
    }

    private static string GetSceneName(GameScene scene)
    {
        switch (scene)
        {
            case GameScene.Title:
                return TitleSceneName;
            case GameScene.Main:
                return MainSceneName;
            case GameScene.Game:
                return GameSceneName;
            default:
                return TitleSceneName;
        }
    }
}
