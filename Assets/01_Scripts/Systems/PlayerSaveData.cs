using System;
using System.Collections.Generic;

[Serializable]
public sealed class PlayerSaveData
{
    public int saveVersion = SaveManager.CurrentSaveVersion;
    public int gold;
    public int hearts;
    public int selectedStageId;
    public bool infiniteMode;
    public List<StageProgressData> stageProgress = new List<StageProgressData>();
    public GameSettingsData settings = new GameSettingsData();
}

[Serializable]
public sealed class StageProgressData
{
    public int stageId;
    public bool unlocked;
    public int bestStars;
    public float bestSimilarity;
    public bool rewardClaimed;
}

[Serializable]
public sealed class GameSettingsData
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float soundVolume = 1f;
    public bool vibrationEnabled = true;
}
