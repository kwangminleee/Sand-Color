using System;
using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    private const string SelectedStageKey = "SandColor.SelectedStage";
    private const string HighestUnlockedStageKey =
        "SandColor.HighestUnlockedStage";
    private const string InfiniteModeKey = "SandColor.InfiniteMode";

    [Header("Scene Components")]
    [SerializeField] private SandSpawner _sandSpawner;
    [SerializeField] private SandPileController _sandPileController;
    [SerializeField] private GameUIController _gameUIController;
    [SerializeField] private ClearPopupUIController _clearPopupUIController;

    [Header("Stage ScriptableObjects")]
    [SerializeField] private StageData _startingStage;
    [SerializeField]
    private StageData[] _stages =
        Array.Empty<StageData>();

    [Header("Stage Evaluation")]
    [SerializeField, Min(0.05f)]
    private float _evaluationInterval = 0.2f;

    [SerializeField, Min(0f)]
    private float _completionHoldSeconds = 0.35f;

    [SerializeField]
    private Color _outlineColor =
        new Color(0.3f, 0.15f, 0.38f, 0.82f);

    [SerializeField, Range(1, 8)]
    private int _outlineThickness = 2;

    private StageData _currentStage;
    private Coroutine _stageRoutine;

    private float _completionStartedAt = -1f;

    private bool _stageReady;
    private bool _stageCleared;
    private bool _infiniteMode;

    public StageData CurrentStage => _currentStage;
    public float CurrentCoverage { get; private set; }
    public float CurrentColorSimilarity { get; private set; }
    public bool StageReady => _stageReady;
    public bool StageCleared => _stageCleared;
    public bool InfiniteMode => _infiniteMode;

    private void Awake()
    {
        Time.timeScale = 1f;

        ResolveCurrentStage();
        InitializeControllers();
        ApplyStageData();
    }

    private void Start()
    {
        _stageRoutine = StartCoroutine(RunStage());
    }

    private void OnEnable()
    {
        if (_sandSpawner == null)
        {
            return;
        }

        _sandSpawner.ColorsChanged += HandleColorsChanged;
    }

    private void OnDisable()
    {
        if (_stageRoutine != null)
        {
            StopCoroutine(_stageRoutine);
            _stageRoutine = null;
        }

        if (_sandSpawner != null)
        {
            _sandSpawner.ColorsChanged -= HandleColorsChanged;
            _sandSpawner.SetInputEnabled(true);
        }

        Time.timeScale = 1f;
    }

    private void InitializeControllers()
    {
        if (_clearPopupUIController == null)
        {
            _clearPopupUIController =
                GetComponentInChildren<ClearPopupUIController>(true);
        }

        if (_gameUIController != null)
        {
            _gameUIController.Initialize(this);
        }

        if (_clearPopupUIController != null)
        {
            _clearPopupUIController.Initialize(this);
            _clearPopupUIController.Close();
        }
    }

    private void ApplyStageData()
    {
        if (_infiniteMode)
        {
            if (_sandPileController != null) _sandPileController.ClearTemplate();
            if (_gameUIController != null)
            {
                _gameUIController.SetInfiniteMode(true);
                _gameUIController.SetProgress(0f);
            }
            return;
        }

        if (_currentStage == null)
        {
            return;
        }

        if (_gameUIController != null) _gameUIController.SetInfiniteMode(false);

        if (_sandSpawner != null)
        {
            _sandSpawner.SetSandPalette(_currentStage.SandPalette);
        }

        if (_gameUIController != null)
        {
            _gameUIController.SetTarget(_currentStage.TargetSprite);
            _gameUIController.SetProgress(0f);
        }

        if (_clearPopupUIController != null)
        {
            _clearPopupUIController.SetTarget(_currentStage.TargetSprite);
        }
    }

    private IEnumerator RunStage()
    {
        // SandSpawner의 Start 초기화가 끝난 다음 목표를 적용합니다.
        yield return null;

        if (_infiniteMode)
        {
            if (_sandSpawner == null || _sandPileController == null || !_sandSpawner.IsInitialized)
            {
                Debug.LogError("무한모드의 모래 생성기 초기화에 실패했습니다.", this);
                yield break;
            }

            _sandPileController.ClearTemplate();
            _sandSpawner.SetInputEnabled(true);
            _stageReady = true;
            RefreshSpawnerColors();
            _stageRoutine = null;
            yield break;
        }

        if (_currentStage == null)
        {
            Debug.LogError(
                "StageManager의 Starting Stage와 Stages 배열에 " +
                "StageDefinition SO를 할당하세요.",
                this);

            yield break;
        }

        if (!TryValidateStage(_currentStage, out string stageError))
        {
            Debug.LogError(stageError, _currentStage);
            yield break;
        }

        if (_sandSpawner == null || _sandPileController == null)
        {
            Debug.LogError(
                "StageManager의 Sand Spawner와 Sand Pile Controller를 " +
                "Inspector에서 할당하세요.",
                this);

            yield break;
        }

        if (!_sandSpawner.IsInitialized)
        {
            Debug.LogError(
                "SandSpawner 초기화에 실패했습니다. " +
                "Particle Prefab과 Touch Area 할당을 확인하세요.",
                _sandSpawner);

            yield break;
        }

        if (!TryReadPixels(
                _currentStage.TemplateMask,
                out Color32[] maskPixels) ||
            !TryReadPixels(
                _currentStage.TargetTexture,
                out Color32[] targetPixels))
        {
            Debug.LogError(
                $"스테이지 {_currentStage.StageNumber}의 " +
                "목표 텍스처를 읽지 못했습니다.",
                _currentStage);

            yield break;
        }

        _stageReady = _sandPileController.ConfigureStageTarget(
            maskPixels,
            _currentStage.TemplateMask.width,
            _currentStage.TemplateMask.height,
            targetPixels,
            _currentStage.TargetTexture.width,
            _currentStage.TargetTexture.height,
            _outlineColor,
            _outlineThickness);

        if (!_stageReady)
        {
            Debug.LogError(
                $"스테이지 {_currentStage.StageNumber}의 " +
                "마스크가 비어 있거나 올바르지 않습니다.",
                _currentStage);

            yield break;
        }

        RefreshSpawnerColors();

        float nextEvaluationAt = 0f;

        while (!_stageCleared)
        {
            if (Time.unscaledTime >= nextEvaluationAt)
            {
                nextEvaluationAt =
                    Time.unscaledTime + _evaluationInterval;

                EvaluateStage();
            }

            yield return null;
        }

        _stageRoutine = null;
    }

    private void EvaluateStage()
    {
        if (!_stageReady ||
            _stageCleared ||
            _currentStage == null ||
            _sandPileController == null)
        {
            return;
        }

        bool evaluated = _sandPileController.EvaluateStageTarget(
            out float coverage,
            out float colorSimilarity);

        if (!evaluated)
        {
            return;
        }

        CurrentCoverage = coverage;
        CurrentColorSimilarity = colorSimilarity;

        if (_gameUIController != null)
        {
            _gameUIController.SetProgress(coverage);
        }

        if (coverage < _currentStage.RequiredFillRatio)
        {
            _completionStartedAt = -1f;
            return;
        }

        if (_completionStartedAt < 0f)
        {
            _completionStartedAt = Time.unscaledTime;
            return;
        }

        float completionDuration =
            Time.unscaledTime - _completionStartedAt;

        if (completionDuration >= _completionHoldSeconds)
        {
            CompleteStage(colorSimilarity);
        }
    }

    private void CompleteStage(float normalizedColorSimilarity)
    {
        if (_stageCleared || _currentStage == null)
        {
            return;
        }

        _stageCleared = true;

        float similarityPercent =
            Mathf.Clamp01(normalizedColorSimilarity) * 100f;

        int stars = CalculateStars(
            _currentStage,
            similarityPercent);

        SaveBest(
            _currentStage.StageNumber,
            stars,
            similarityPercent);

        if (_sandSpawner != null)
        {
            _sandSpawner.SetInputEnabled(false);
        }

        if (_clearPopupUIController != null)
        {
            _clearPopupUIController.Open(
                stars,
                similarityPercent);
        }

        Time.timeScale = 0f;
    }

    private void HandleColorsChanged(
        Color currentColor,
        Color nextColor,
        int remainingParticles)
    {
        if (_gameUIController == null)
        {
            return;
        }

        _gameUIController.SetColors(
            currentColor,
            nextColor);
    }

    private void RefreshSpawnerColors()
    {
        if (_sandSpawner == null)
        {
            return;
        }

        HandleColorsChanged(
            _sandSpawner.CurrentColor,
            _sandSpawner.NextColor,
            _sandSpawner.RemainingParticles);
    }

    public void SkipColor()
    {
        if (_stageCleared || _sandSpawner == null)
        {
            return;
        }

        _sandSpawner.SkipColor();
    }

    public StageData GetStage(int stageNumber)
    {
        if (_stages == null)
        {
            return null;
        }

        for (int index = 0; index < _stages.Length; index++)
        {
            StageData stage = _stages[index];

            if (stage != null &&
                stage.StageNumber == stageNumber)
            {
                return stage;
            }
        }

        return null;
    }

    public void SelectStage(int stageNumber)
    {
        StageData selectedStage = GetStage(stageNumber);

        if (selectedStage == null)
        {
            Debug.LogError(
                $"StageManager에 스테이지 {stageNumber} SO가 " +
                "할당되지 않았습니다.",
                this);

            return;
        }

        PlayerPrefs.SetInt(
            SelectedStageKey,
            selectedStage.StageNumber);

        PlayerPrefs.Save();
    }

    public int GetBestStars(int stageNumber)
    {
        return Mathf.Clamp(
            PlayerPrefs.GetInt(StarsKey(stageNumber), 0),
            0,
            3);
    }

    public float GetBestSimilarity(int stageNumber)
    {
        return Mathf.Clamp(
            PlayerPrefs.GetFloat(
                SimilarityKey(stageNumber),
                0f),
            0f,
            100f);
    }

    public void GoToMain()
    {
        Time.timeScale = 1f;

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadMain();
        }
    }

    public void GoToTitle()
    {
        Time.timeScale = 1f;

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadTitle();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.ReloadCurrentScene();
        }
    }

    public void GoToNextStage()
    {
        if (_currentStage == null)
        {
            GoToMain();
            return;
        }

        StageData nextStage = GetStage(_currentStage.StageNumber + 1);
        if (nextStage == null)
        {
            GoToMain();
            return;
        }

        SelectStage(nextStage.StageNumber);
        RestartGame();
    }

    private void ResolveCurrentStage()
    {
        // 게임 씬은 저장된 이전 선택값과 관계없이 스테이지 0(무한모드)로 시작합니다.
        _infiniteMode = true;
        _currentStage = null;

        PlayerPrefs.SetInt(SelectedStageKey, 0);
        PlayerPrefs.SetInt(InfiniteModeKey, 1);
        PlayerPrefs.Save();
    }

    private static bool TryReadPixels(
        Texture2D source,
        out Color32[] pixels)
    {
        pixels = null;

        if (source == null)
        {
            return false;
        }

        try
        {
            pixels = source.GetPixels32();

            return pixels != null &&
                   pixels.Length == source.width * source.height;
        }
        catch (UnityException)
        {
            // Read/Write가 꺼져 있으면 RenderTexture로 읽습니다.
        }

        RenderTexture previousRenderTexture =
            RenderTexture.active;

        RenderTexture temporaryRenderTexture =
            RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);

        Texture2D readableTexture = null;

        try
        {
            Graphics.Blit(
                source,
                temporaryRenderTexture);

            RenderTexture.active =
                temporaryRenderTexture;

            readableTexture = new Texture2D(
                source.width,
                source.height,
                TextureFormat.RGBA32,
                false);

            readableTexture.ReadPixels(
                new Rect(
                    0f,
                    0f,
                    source.width,
                    source.height),
                0,
                0,
                false);

            readableTexture.Apply(false, false);

            pixels = readableTexture.GetPixels32();

            return pixels != null &&
                   pixels.Length == source.width * source.height;
        }
        finally
        {
            RenderTexture.active =
                previousRenderTexture;

            RenderTexture.ReleaseTemporary(
                temporaryRenderTexture);

            if (readableTexture != null)
            {
                Destroy(readableTexture);
            }
        }
    }

    private static bool TryValidateStage(
        StageData stage,
        out string error)
    {
        if (stage == null)
        {
            error = "StageDefinition SO가 할당되지 않았습니다.";
            return false;
        }

        if (stage.TargetSprite == null)
        {
            error =
                $"스테이지 {stage.StageNumber}의 " +
                "Target Sprite가 필요합니다.";

            return false;
        }

        if (stage.TargetTexture == null)
        {
            error =
                $"스테이지 {stage.StageNumber}의 " +
                "Target Texture가 필요합니다.";

            return false;
        }

        if (stage.TemplateMask == null)
        {
            error =
                $"스테이지 {stage.StageNumber}의 " +
                "Template Mask가 필요합니다.";

            return false;
        }

        if (stage.SandPalette == null ||
            stage.SandPalette.Length == 0)
        {
            error =
                $"스테이지 {stage.StageNumber}에 " +
                "모래 색상을 하나 이상 설정하세요.";

            return false;
        }

        if (stage.ThreeStarThreshold <
            stage.TwoStarThreshold)
        {
            error =
                $"스테이지 {stage.StageNumber}의 " +
                "별 기준값 순서가 올바르지 않습니다.";

            return false;
        }

        error = string.Empty;
        return true;
    }

    private static int CalculateStars(
        StageData stage,
        float similarityPercent)
    {
        float score = float.IsNaN(similarityPercent)
            ? 0f
            : Mathf.Clamp(
                similarityPercent,
                0f,
                100f);

        if (score >= stage.ThreeStarThreshold)
        {
            return 3;
        }

        if (score >= stage.TwoStarThreshold)
        {
            return 2;
        }

        return 1;
    }

    private static void SaveBest(
        int stageNumber,
        int stars,
        float similarityPercent)
    {
        int safeStageNumber =
            Mathf.Max(1, stageNumber);

        int safeStars =
            Mathf.Clamp(stars, 1, 3);

        float safeSimilarity =
            Mathf.Clamp(
                similarityPercent,
                0f,
                100f);

        bool improved = false;

        string starsKey =
            StarsKey(safeStageNumber);

        int savedStars = Mathf.Clamp(
            PlayerPrefs.GetInt(starsKey, 0),
            0,
            3);

        if (safeStars > savedStars)
        {
            PlayerPrefs.SetInt(
                starsKey,
                safeStars);

            improved = true;
        }

        string similarityKey =
            SimilarityKey(safeStageNumber);

        float savedSimilarity = Mathf.Clamp(
            PlayerPrefs.GetFloat(
                similarityKey,
                0f),
            0f,
            100f);

        if (safeSimilarity > savedSimilarity)
        {
            PlayerPrefs.SetFloat(
                similarityKey,
                safeSimilarity);

            improved = true;
        }

        int highestUnlockedStage =
            PlayerPrefs.GetInt(
                HighestUnlockedStageKey,
                1);

        if (safeStageNumber + 1 >
            highestUnlockedStage)
        {
            PlayerPrefs.SetInt(
                HighestUnlockedStageKey,
                safeStageNumber + 1);

            improved = true;
        }

        if (improved)
        {
            PlayerPrefs.Save();
        }
    }

    private static string StarsKey(int stageNumber)
    {
        return
            $"SandColor.Stage.{Mathf.Max(1, stageNumber)}" +
            ".BestStars";
    }

    private static string SimilarityKey(int stageNumber)
    {
        return
            $"SandColor.Stage.{Mathf.Max(1, stageNumber)}" +
            ".BestSimilarity";
    }

    private void OnValidate()
    {
        _stages ??= Array.Empty<StageData>();

        _evaluationInterval =
            Mathf.Max(
                0.05f,
                _evaluationInterval);

        _completionHoldSeconds =
            Mathf.Max(
                0f,
                _completionHoldSeconds);

        _outlineThickness =
            Mathf.Clamp(
                _outlineThickness,
                1,
                8);
    }
}
