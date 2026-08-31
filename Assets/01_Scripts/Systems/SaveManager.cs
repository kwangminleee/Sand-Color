using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    public const int CurrentSaveVersion = 1;

    private const string SaveFileName = "save.json";
    private const string BackupFileName = "save.backup.json";
    private const string LegacySelectedStageKey = "SandColor.SelectedStage";
    private const string LegacyInfiniteModeKey = "SandColor.InfiniteMode";
    private const string LegacyHighestUnlockedStageKey = "SandColor.HighestUnlockedStage";

    private static PlayerSaveData _data;
    private static bool _initialized;
    private static bool _legacyMigrationCompleted;

    public static PlayerSaveData Data
    {
        get
        {
            EnsureLoaded();
            return _data;
        }
    }

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static void Initialize(IEnumerable<StageData> stages)
    {
        EnsureLoaded();

        if (_legacyMigrationCompleted)
        {
            EnsureStageEntries(stages);
            return;
        }

        MigrateLegacyPlayerPrefs(stages);
        _legacyMigrationCompleted = true;
        Save();
    }

    public static void SelectStage(StageData stage)
    {
        if (stage == null)
        {
            return;
        }

        EnsureLoaded();
        _data.selectedStageId = stage.Id;
        _data.infiniteMode = false;
        Save();
    }

    public static void SelectInfiniteMode()
    {
        EnsureLoaded();
        _data.selectedStageId = 0;
        _data.infiniteMode = true;
        Save();
    }

    public static StageProgressData GetStageProgress(StageData stage)
    {
        if (stage == null)
        {
            return null;
        }

        EnsureLoaded();
        return GetOrCreateStageProgress(stage.Id, stage.StageNumber == 1);
    }

    public static void RecordStageResult(StageData stage, int stars, float similarityPercent)
    {
        StageProgressData progress = GetStageProgress(stage);
        if (progress == null)
        {
            return;
        }

        progress.unlocked = true;
        progress.bestStars = Mathf.Max(progress.bestStars, Mathf.Clamp(stars, 1, 3));
        progress.bestSimilarity = Mathf.Max(progress.bestSimilarity, Mathf.Clamp(similarityPercent, 0f, 100f));
        Save();
    }

    public static void UnlockStage(StageData stage)
    {
        StageProgressData progress = GetStageProgress(stage);
        if (progress != null && !progress.unlocked)
        {
            progress.unlocked = true;
            Save();
        }
    }

    public static void Save()
    {
        EnsureLoaded();
        NormalizeData();

        string savePath = SavePath;
        string temporaryPath = savePath + ".tmp";
        string backupPath = Path.Combine(Application.persistentDataPath, BackupFileName);

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(_data, true));

            if (File.Exists(savePath))
            {
                File.Replace(temporaryPath, savePath, backupPath, true);
            }
            else
            {
                File.Move(temporaryPath, savePath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"세이브 파일을 저장하지 못했습니다: {exception.Message}");

            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static void ResetSave()
    {
        _data = CreateDefaultData();
        _initialized = true;
        _legacyMigrationCompleted = true;
        Save();
    }

    private static void EnsureLoaded()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _data = TryLoad(SavePath) ?? TryLoad(Path.Combine(Application.persistentDataPath, BackupFileName));
        _legacyMigrationCompleted = _data != null;
        _data ??= CreateDefaultData();
        NormalizeData();
    }

    private static PlayerSaveData TryLoad(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<PlayerSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"세이브 파일을 읽지 못했습니다 ({path}): {exception.Message}");
            return null;
        }
    }

    private static PlayerSaveData CreateDefaultData()
    {
        return new PlayerSaveData();
    }

    private static void NormalizeData()
    {
        _data ??= CreateDefaultData();
        _data.saveVersion = CurrentSaveVersion;
        _data.gold = Mathf.Max(0, _data.gold);
        _data.hearts = Mathf.Max(0, _data.hearts);
        _data.stageProgress ??= new List<StageProgressData>();
        _data.settings ??= new GameSettingsData();
    }

    private static void EnsureStageEntries(IEnumerable<StageData> stages)
    {
        if (stages == null)
        {
            return;
        }

        foreach (StageData stage in stages)
        {
            if (stage != null)
            {
                GetOrCreateStageProgress(stage.Id, stage.StageNumber == 1);
            }
        }
    }

    private static StageProgressData GetOrCreateStageProgress(int stageId, bool unlockedByDefault)
    {
        for (int index = 0; index < _data.stageProgress.Count; index++)
        {
            StageProgressData progress = _data.stageProgress[index];
            if (progress != null && progress.stageId == stageId)
            {
                return progress;
            }
        }

        StageProgressData created = new StageProgressData
        {
            stageId = stageId,
            unlocked = unlockedByDefault
        };
        _data.stageProgress.Add(created);
        return created;
    }

    private static void MigrateLegacyPlayerPrefs(IEnumerable<StageData> stages)
    {
        int selectedStageNumber = PlayerPrefs.GetInt(LegacySelectedStageKey, 1);
        int highestUnlocked = Mathf.Max(1, PlayerPrefs.GetInt(LegacyHighestUnlockedStageKey, 1));
        _data.infiniteMode = PlayerPrefs.GetInt(LegacyInfiniteModeKey, 0) == 1 || selectedStageNumber <= 0;

        if (stages == null)
        {
            return;
        }

        foreach (StageData stage in stages)
        {
            if (stage == null)
            {
                continue;
            }

            StageProgressData progress = GetOrCreateStageProgress(stage.Id, stage.StageNumber <= highestUnlocked);
            progress.unlocked |= stage.StageNumber <= highestUnlocked;
            progress.bestStars = Mathf.Max(progress.bestStars, Mathf.Clamp(PlayerPrefs.GetInt(LegacyStarsKey(stage.StageNumber), 0), 0, 3));
            progress.bestSimilarity = Mathf.Max(progress.bestSimilarity, Mathf.Clamp(PlayerPrefs.GetFloat(LegacySimilarityKey(stage.StageNumber), 0f), 0f, 100f));

            if (!_data.infiniteMode && stage.StageNumber == selectedStageNumber)
            {
                _data.selectedStageId = stage.Id;
            }
        }
    }

    private static string LegacyStarsKey(int stageNumber) => $"SandColor.Stage.{Mathf.Max(1, stageNumber)}.BestStars";
    private static string LegacySimilarityKey(int stageNumber) => $"SandColor.Stage.{Mathf.Max(1, stageNumber)}.BestSimilarity";
}
