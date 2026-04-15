/// <summary>
/// 全局关卡数据持久化工具（纯静态类，基于 PlayerPrefs）。
/// 负责保存/读取每关最佳星星、金币，以及星星消耗/章节解锁状态。
/// </summary>
public static class LevelDataManager
{
    // ============ PlayerPrefs 键名前缀 ============
    private const string BestStarsPrefix    = "BestStars_";
    private const string BestCoinsPrefix    = "BestCoins_";
    private const string CompletedPrefix    = "Completed_";
    private const string SpentStarsKey      = "SpentStars";
    private const string ChapterUnlockPrefix = "ChapterUnlocked_";

    // ============ 关卡成绩 ============

    /// <summary>
    /// 保存本关成绩（仅当新值 > 已存值时更新），并标记关卡已完成。
    /// </summary>
    public static void SaveLevelResult(string levelName, int stars, int coins)
    {
        int prevStars = UnityEngine.PlayerPrefs.GetInt(BestStarsPrefix + levelName, 0);
        if (stars > prevStars)
            UnityEngine.PlayerPrefs.SetInt(BestStarsPrefix + levelName, stars);

        int prevCoins = UnityEngine.PlayerPrefs.GetInt(BestCoinsPrefix + levelName, 0);
        if (coins > prevCoins)
            UnityEngine.PlayerPrefs.SetInt(BestCoinsPrefix + levelName, coins);

        UnityEngine.PlayerPrefs.SetInt(CompletedPrefix + levelName, 1);
        UnityEngine.PlayerPrefs.Save();
    }

    public static int GetBestStars(string levelName)
        => UnityEngine.PlayerPrefs.GetInt(BestStarsPrefix + levelName, 0);

    public static int GetBestCoins(string levelName)
        => UnityEngine.PlayerPrefs.GetInt(BestCoinsPrefix + levelName, 0);

    public static bool IsLevelCompleted(string levelName)
        => UnityEngine.PlayerPrefs.GetInt(CompletedPrefix + levelName, 0) == 1;

    // ============ 汇总 ============

    /// <summary>全部关卡累计最佳星星总数（已获）。</summary>
    public static int GetTotalStars(string[] levelNames)
    {
        int total = 0;
        foreach (var name in levelNames)
            total += GetBestStars(name);
        return total;
    }

    /// <summary>全部关卡累计最佳金币总数。</summary>
    public static int GetTotalCoins(string[] levelNames)
    {
        int total = 0;
        foreach (var name in levelNames)
            total += GetBestCoins(name);
        return total;
    }

    // ============ 消耗 & 章节解锁 ============

    /// <summary>已消耗的星星总数（用于解锁章节）。</summary>
    public static int GetSpentStars()
        => UnityEngine.PlayerPrefs.GetInt(SpentStarsKey, 0);

    /// <summary>可用星星数 = 已获总数 - 已消耗总数。</summary>
    public static int GetAvailableStars(string[] levelNames)
        => GetTotalStars(levelNames) - GetSpentStars();

    /// <summary>
    /// 消耗星星解锁章节。
    /// </summary>
    /// <returns>解锁成功返回 true；可用星星不足返回 false。</returns>
    public static bool TryUnlockChapter(int chapterIndex, int cost, string[] levelNames)
    {
        if (IsChapterUnlocked(chapterIndex))
            return true; // 已解锁，不重复消耗

        int available = GetAvailableStars(levelNames);
        if (available < cost)
            return false;

        int spent = GetSpentStars();
        UnityEngine.PlayerPrefs.SetInt(SpentStarsKey, spent + cost);
        UnityEngine.PlayerPrefs.SetInt(ChapterUnlockPrefix + chapterIndex, 1);
        UnityEngine.PlayerPrefs.Save();
        return true;
    }

    /// <summary>检查章节是否已通过消耗解锁（第 0 章始终视为已解锁）。</summary>
    public static bool IsChapterUnlocked(int chapterIndex)
    {
        if (chapterIndex <= 0) return true;
        return UnityEngine.PlayerPrefs.GetInt(ChapterUnlockPrefix + chapterIndex, 0) == 1;
    }

    // ============ 调试 ============

    /// <summary>清除所有存档数据（仅用于开发调试）。</summary>
    public static void ClearAllData(string[] levelNames)
    {
        foreach (var name in levelNames)
        {
            UnityEngine.PlayerPrefs.DeleteKey(BestStarsPrefix  + name);
            UnityEngine.PlayerPrefs.DeleteKey(BestCoinsPrefix  + name);
            UnityEngine.PlayerPrefs.DeleteKey(CompletedPrefix  + name);
        }
        UnityEngine.PlayerPrefs.DeleteKey(SpentStarsKey);
        // 章节解锁状态也清除
        for (int i = 1; i < 20; i++)
            UnityEngine.PlayerPrefs.DeleteKey(ChapterUnlockPrefix + i);
        UnityEngine.PlayerPrefs.Save();
    }
}
