using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 章节配置（ScriptableObject），在 Inspector 中配置章节结构和解锁条件。
/// 创建方式：Project 窗口右键 → Create → Game → ChapterConfig
/// </summary>
[CreateAssetMenu(fileName = "ChapterConfig", menuName = "Game/ChapterConfig")]
public class ChapterConfig : ScriptableObject
{
    public ChapterDefinition[] chapters;

    [System.Serializable]
    public class ChapterDefinition
    {
        public string chapterName;              // 如 "第一章"
        public int unlockCostStars;             // 消耗多少星星解锁本章（第一章设为 0）
        public int starsToUnlockNextLevel = 2;  // 章节内解锁下一关所需的当前关星星数
        public string[] levelNames;             // 本章包含的关卡场景名
    }

    /// <summary>返回所有章节的全部关卡名（扁平数组）。</summary>
    public string[] GetAllLevelNames()
    {
        var list = new List<string>();
        foreach (var chapter in chapters)
        {
            if (chapter.levelNames != null)
                list.AddRange(chapter.levelNames);
        }
        return list.ToArray();
    }

    /// <summary>
    /// 检查指定章节是否已解锁。
    /// 第 0 章始终免费；其他章节需通过消耗星星手动解锁。
    /// </summary>
    public bool IsChapterUnlocked(int chapterIndex)
        => LevelDataManager.IsChapterUnlocked(chapterIndex);

    /// <summary>
    /// 消耗星星解锁章节。
    /// </summary>
    /// <returns>解锁成功 true；可用星星不足 false。</returns>
    public bool TryUnlockChapter(int chapterIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= chapters.Length) return false;
        return LevelDataManager.TryUnlockChapter(
            chapterIndex,
            chapters[chapterIndex].unlockCostStars,
            GetAllLevelNames()
        );
    }

    /// <summary>
    /// 检查指定关卡是否可进入。
    /// 规则：章节已解锁 + (章节内第一关 或 前一关最佳星星 ≥ starsToUnlockNextLevel)
    /// </summary>
    public bool IsLevelUnlocked(string levelName)
    {
        for (int c = 0; c < chapters.Length; c++)
        {
            var chapter = chapters[c];
            if (chapter.levelNames == null) continue;

            for (int l = 0; l < chapter.levelNames.Length; l++)
            {
                if (chapter.levelNames[l] != levelName) continue;

                if (!IsChapterUnlocked(c)) return false;
                if (l == 0) return true;

                string prevLevel = chapter.levelNames[l - 1];
                return LevelDataManager.GetBestStars(prevLevel) >= chapter.starsToUnlockNextLevel;
            }
        }
        return true; // 不在配置中的关卡（如测试关）默认可进入
    }

    /// <summary>查找关卡所属章节索引，未找到返回 -1。</summary>
    public int GetChapterIndexForLevel(string levelName)
    {
        for (int c = 0; c < chapters.Length; c++)
        {
            if (chapters[c].levelNames == null) continue;
            foreach (var name in chapters[c].levelNames)
                if (name == levelName) return c;
        }
        return -1;
    }
}
