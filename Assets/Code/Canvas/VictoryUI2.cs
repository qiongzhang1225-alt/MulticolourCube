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

    [Header("关卡设置")]
    public string nextSceneName;

    [Header("金币UI")]
    public TextMeshProUGUI coinCountText;

    [Header("死亡次数UI（可选）")]
    public TextMeshProUGUI deathCountText;  // 显示本关死亡次数

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

        // 时间计时
        if (timeText != null && GameTimer.Instance != null)
        {
            GameTimer.Instance.StopTimer();
            timeText.text = GameTimer.Instance.GetFormattedTime();
        }

        // ========== 星星逻辑 ==========
        int starCount = CollectableStar.CollectedCount;
        for (int i = 0; i < starSlots.Length; i++)
        {
            bool earned = i < starCount;
            starSlots[i].fullStar.enabled = earned;
            starSlots[i].emptyStar.enabled = !earned;
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
        int savedStars = CollectableStar.CollectedCount;
        int savedCoins = CollectableCoin.CollectedCount;
        Debug.Log("[VictoryUI] 保存成绩: " + currentLevel + " stars=" + savedStars + " coins=" + savedCoins);
        LevelDataManager.SaveLevelResult(currentLevel, savedStars, savedCoins);
        Debug.Log("[VictoryUI] 保存后验证: BestStars=" + LevelDataManager.GetBestStars(currentLevel));

        // 重置统计
        CollectableStar.CollectedCount = 0;
        CollectableCoin.CollectedCount = 0;
    }

    void Update()
    {
        // 胜利面板显示时，按 Escape 返回关卡选择
        if (victoryPanel != null && victoryPanel.activeSelf
            && Input.GetKeyDown(KeyCode.Escape))
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
}
