using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 关卡选择界面控制器。
/// 负责：关卡解锁刷新、全局星星/金币显示、章节消耗解锁按钮。
/// </summary>
public class LevelSelect : MonoBehaviour
{
    [Header("章节配置")]
    public ChapterConfig chapterConfig;

    [Header("全局资源显示（右上角）")]
    public TextMeshProUGUI totalStarText;    // 显示可用星星数，如 "⭐ 5"
    public TextMeshProUGUI totalCoinText;    // 显示累计金币数，如 "🪙 30"

    [Header("章节解锁按钮（每章一个，index 对应 chapters[]）")]
    public ChapterUnlockButton[] chapterUnlockButtons;

    [System.Serializable]
    public class ChapterUnlockButton
    {
        public int chapterIndex;            // 对应 ChapterConfig.chapters 的索引
        public Button unlockButton;         // 点击后消耗星星解锁
        public TextMeshProUGUI costText;    // 显示解锁费用，如 "解锁：12⭐"
        public GameObject lockedPanel;      // 锁定状态显示面板
        public GameObject unlockedPanel;    // 已解锁状态显示面板
    }

    // 缓存找到的所有 LevelButton（含非活跃层级中的）
    private LevelButton[] cachedLevelButtons;

    void Start()
    {
        // 绑定章节解锁按钮事件
        foreach (var cu in chapterUnlockButtons)
        {
            if (cu.unlockButton == null) continue;
            int idx = cu.chapterIndex; // 捕获
            cu.unlockButton.onClick.RemoveAllListeners();
            cu.unlockButton.onClick.AddListener(() => OnUnlockChapterClicked(idx));
        }

        // 预缓存所有 LevelButton（包括在未激活面板中的）
        CacheLevelButtons();

        Refresh();
    }

    /// <summary>
    /// 查找场景中所有 LevelButton，包括在未激活 GameObject 下的。
    /// FindObjectsOfType 只能找到活跃物体，所以用 Resources.FindObjectsOfTypeAll 并过滤。
    /// </summary>
    void CacheLevelButtons()
    {
        var all = Resources.FindObjectsOfTypeAll<LevelButton>();
        var list = new List<LevelButton>();
        foreach (var lb in all)
        {
            // 只保留当前已加载场景中的，排除 Prefab 资源
            if (lb.gameObject.scene.isLoaded)
                list.Add(lb);
        }
        cachedLevelButtons = list.ToArray();
        Debug.Log("[LevelSelect] 缓存了 " + cachedLevelButtons.Length + " 个 LevelButton（含非活跃层级）");
    }

    /// <summary>刷新所有 UI（关卡按钮 + 全局显示 + 章节解锁状态）。</summary>
    public void Refresh()
    {
        RefreshGlobalDisplay();
        RefreshAllLevelButtons();
        RefreshChapterUnlockButtons();
    }

    // ──────────────────────────────────────────
    // 全局星星/金币显示
    // ──────────────────────────────────────────

    void RefreshGlobalDisplay()
    {
        if (chapterConfig == null) return;
        string[] allLevels = chapterConfig.GetAllLevelNames();

        if (totalStarText != null)
        {
            int available = LevelDataManager.GetAvailableStars(allLevels);
            int total     = LevelDataManager.GetTotalStars(allLevels);
            totalStarText.text = available + " / " + total;
        }

        if (totalCoinText != null)
        {
            int totalCoins = LevelDataManager.GetTotalCoins(allLevels);
            totalCoinText.text = totalCoins.ToString();
        }
    }

    // ──────────────────────────────────────────
    // 关卡按钮刷新
    // ──────────────────────────────────────────

    void RefreshAllLevelButtons()
    {
        // 如果缓存为空，重新查找
        if (cachedLevelButtons == null || cachedLevelButtons.Length == 0)
            CacheLevelButtons();

        foreach (var lb in cachedLevelButtons)
        {
            if (lb == null || string.IsNullOrEmpty(lb.levelName)) continue;

            bool unlocked = chapterConfig != null && chapterConfig.IsLevelUnlocked(lb.levelName);

            // 交互状态
            if (lb.button != null)
                lb.button.interactable = unlocked;

            // 锁定遮罩
            if (lb.lockOverlay != null)
                lb.lockOverlay.SetActive(!unlocked);

            // 星星槽位
            int bestStars = LevelDataManager.GetBestStars(lb.levelName);
            Debug.Log("[LevelSelect] " + lb.levelName + " → BestStars=" + bestStars + " unlocked=" + unlocked);
            lb.RefreshStars(bestStars);

            // 金币文字
            if (lb.coinText != null)
                lb.coinText.text = LevelDataManager.GetBestCoins(lb.levelName).ToString();

            // 绑定点击（保证只绑定一次）
            if (lb.button != null)
            {
                string name = lb.levelName;
                lb.button.onClick.RemoveAllListeners();
                lb.button.onClick.AddListener(() => LoadLevel(name));
            }
        }
    }

    // ──────────────────────────────────────────
    // 章节解锁按钮刷新
    // ──────────────────────────────────────────

    void RefreshChapterUnlockButtons()
    {
        if (chapterConfig == null) return;
        string[] allLevels = chapterConfig.GetAllLevelNames();
        int available = LevelDataManager.GetAvailableStars(allLevels);

        foreach (var cu in chapterUnlockButtons)
        {
            if (cu.chapterIndex <= 0 || cu.chapterIndex >= chapterConfig.chapters.Length)
                continue;

            bool isUnlocked = LevelDataManager.IsChapterUnlocked(cu.chapterIndex);
            int cost = chapterConfig.chapters[cu.chapterIndex].unlockCostStars;

            if (cu.lockedPanel   != null) cu.lockedPanel.SetActive(!isUnlocked);
            if (cu.unlockedPanel != null) cu.unlockedPanel.SetActive(isUnlocked);

            if (cu.costText != null)
                cu.costText.text = "解锁：" + cost + "⭐";

            if (cu.unlockButton != null)
                cu.unlockButton.interactable = !isUnlocked && available >= cost;
        }
    }

    // ──────────────────────────────────────────
    // 事件处理
    // ──────────────────────────────────────────

    void OnUnlockChapterClicked(int chapterIndex)
    {
        if (chapterConfig == null) return;
        bool success = chapterConfig.TryUnlockChapter(chapterIndex);
        if (success)
        {
            Refresh();
        }
        else
        {
            Debug.Log("星星不足，无法解锁第 " + chapterIndex + " 章");
        }
    }

    public void LoadLevel(string levelName)
    {
        Time.timeScale = 1f; // chapt1 打开面板时会冻结时间，进入关卡前必须恢复
        SceneManager.LoadScene(levelName);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("Main_menu");
    }
}
