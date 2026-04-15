using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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

        Refresh();
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
            // 显示：可用 / 已获
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
        LevelButton[] levelButtons = FindObjectsOfType<LevelButton>();
        foreach (var lb in levelButtons)
        {
            if (string.IsNullOrEmpty(lb.levelName)) continue;

            bool unlocked = chapterConfig != null && chapterConfig.IsLevelUnlocked(lb.levelName);

            // 交互状态
            if (lb.button != null)
                lb.button.interactable = unlocked;

            // 锁定遮罩
            if (lb.lockOverlay != null)
                lb.lockOverlay.SetActive(!unlocked);

            // 星星槽位
            int bestStars = LevelDataManager.GetBestStars(lb.levelName);
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

            // 切换面板
            if (cu.lockedPanel   != null) cu.lockedPanel.SetActive(!isUnlocked);
            if (cu.unlockedPanel != null) cu.unlockedPanel.SetActive(isUnlocked);

            // 费用文字
            if (cu.costText != null)
                cu.costText.text = "解锁：" + cost + "⭐";

            // 按钮交互（星星不足时置灰）
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
            Refresh(); // 解锁后刷新全部 UI
        }
        else
        {
            Debug.Log("星星不足，无法解锁第 " + chapterIndex + " 章");
        }
    }

    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("Main_menu");
    }
}
