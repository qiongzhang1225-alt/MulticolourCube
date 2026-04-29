using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class VictoryUI : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject victoryPanel;
    public Button nextLevelButton;
    public Button retryButton;
    public Button backToSelectButton;       // 新增：返回关卡选择按钮
    public TextMeshProUGUI timeText;
    public Image[] starImages;

    [Header("星星展示槽")]
    public StarSlot[] starSlots;

    [Header("Boss 模式（勾选后用爱心代替星星，显示剩余 HP）")]
    [Tooltip("勾选后：隐藏 starSlots、显示 heartSlots，并按 BossPlayerHP.CurrentHP 填充")]
    public bool bossMode = false;
    [Tooltip("Boss 模式下要点亮/置灰的爱心槽（按 HP 顺序，长度建议 = MaxHP）")]
    public HeartSlot[] heartSlots;
    [Tooltip("Boss 模式下要整体隐藏的 GameObject（如原星星根节点 Star1/2/3）；可选")]
    public GameObject[] hideOnBossMode;

    [Header("关卡设置")]
    public string nextSceneName;

    [Header("金币UI")]
    public TextMeshProUGUI coinCountText;

    [Header("死亡次数UI（可选）")]
    public TextMeshProUGUI deathCountText;  // 显示本关死亡次数

    [Header("胜利 BGM（可选）")]
    [Tooltip("通关时播放的独立 BGM。拖入 AudioClip 即可。")]
    public AudioClip victoryBGM;

    void Start()
    {
        victoryPanel.SetActive(false);
        nextLevelButton.onClick.AddListener(LoadNextLevel);
        retryButton.onClick.AddListener(ReloadCurrentLevel);

        // 返回关卡选择按钮（可选）
        if (backToSelectButton != null)
            backToSelectButton.onClick.AddListener(BackToLevelSelect);
    }

    public void ShowVictory()
    {
        victoryPanel.SetActive(true);
        Time.timeScale = 0f;

        // ========== 胜利 BGM ==========
        if (victoryBGM != null && BGMManager.Instance != null)
        {
            BGMManager.Instance.PlayVictoryBGM(victoryBGM);
        }

        // 时间计时
        if (timeText != null && GameTimer.Instance != null)
        {
            GameTimer.Instance.StopTimer();
            timeText.text = GameTimer.Instance.GetFormattedTime();
        }

        // ========== 星星 / 爱心 显示 ==========
        if (bossMode)
        {
            // 隐藏星星槽
            if (starSlots != null)
            {
                for (int i = 0; i < starSlots.Length; i++)
                {
                    if (starSlots[i] == null) continue;
                    if (starSlots[i].fullStar != null) starSlots[i].fullStar.enabled = false;
                    if (starSlots[i].emptyStar != null) starSlots[i].emptyStar.enabled = false;
                }
            }
            // 隐藏配置的根节点（例如 Star1/Star2/Star3）
            if (hideOnBossMode != null)
            {
                for (int i = 0; i < hideOnBossMode.Length; i++)
                    if (hideOnBossMode[i] != null) hideOnBossMode[i].SetActive(false);
            }
            // 显示生命值
            int hp = BossPlayerHP.Instance != null ? BossPlayerHP.Instance.CurrentHP : 0;
            if (heartSlots != null)
            {
                for (int i = 0; i < heartSlots.Length; i++)
                {
                    if (heartSlots[i] == null) continue;
                    bool full = i < hp;
                    if (heartSlots[i].fullHeart != null) heartSlots[i].fullHeart.enabled = full;
                    if (heartSlots[i].emptyHeart != null) heartSlots[i].emptyHeart.enabled = !full;
                }
            }
        }
        else
        {
            int starCount = CollectableStar.CollectedCount;
            for (int i = 0; i < starSlots.Length; i++)
            {
                bool earned = i < starCount;
                starSlots[i].fullStar.enabled = earned;
                starSlots[i].emptyStar.enabled = !earned;
            }
        }

        // ========== 显示金币数量 ==========
        if (coinCountText != null)
        {
            coinCountText.text = "Coin: " + CollectableCoin.CollectedCount;
        }

        // ========== 显示死亡次数 ==========
        if (deathCountText != null)
        {
            int deaths = DeathEffectUI.DeathCount;
            if (deaths == 0)
                deathCountText.text = "零死通关！";
            else
                deathCountText.text = "Death: " + deaths;
        }

        // ========== 保存本关最佳成绩到 PlayerPrefs ==========
        string currentLevel = SceneManager.GetActiveScene().name;
        // Boss 关用剩余 HP 顶替"星数"（最多 maxHP，越多越好）；普通关沿用收集星星数
        int savedStars;
        if (bossMode)
        {
            savedStars = BossPlayerHP.Instance != null ? BossPlayerHP.Instance.CurrentHP : 0;
        }
        else
        {
            savedStars = CollectableStar.CollectedCount;
        }
        int savedCoins = CollectableCoin.CollectedCount;
        Debug.Log("[VictoryUI] 保存成绩: " + currentLevel + " stars=" + savedStars + " coins=" + savedCoins + " bossMode=" + bossMode);
        LevelDataManager.SaveLevelResult(currentLevel, savedStars, savedCoins);
        Debug.Log("[VictoryUI] 保存后验证: BestStars=" + LevelDataManager.GetBestStars(currentLevel));

        // 重置统计
        CollectableStar.CollectedCount = 0;
        CollectableCoin.CollectedCount = 0;
    }

    void Update()
    {
        // 胜利面板显示时，按 Escape / 手柄 Start 返回关卡选择
        if (victoryPanel != null && victoryPanel.activeSelf
            && InputAdapter.PausePressed)
        {
            BackToLevelSelect();
        }
    }

    void LoadNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    void ReloadCurrentLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        CollectableStar.CollectedCount = 0;
        CollectableCoin.CollectedCount = 0;
    }

    void BackToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelect");
    }

    [System.Serializable]
    public class StarSlot
    {
        public Image fullStar;
        public Image emptyStar;
    }

    [System.Serializable]
    public class HeartSlot
    {
        public Image fullHeart;
        public Image emptyHeart;
    }
}
