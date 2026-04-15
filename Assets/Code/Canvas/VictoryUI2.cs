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
    public TextMeshProUGUI timeText;
    public Image[] starImages;

    [Header("星星展示槽")]
    public StarSlot[] starSlots;

    [Header("关卡设置")]
    public string nextSceneName;

    [Header("金币UI")] // 新增
    public TextMeshProUGUI coinCountText; // 拖入显示金币的文本

    void Start()
    {
        victoryPanel.SetActive(false);
        nextLevelButton.onClick.AddListener(LoadNextLevel);
        retryButton.onClick.AddListener(ReloadCurrentLevel);
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

        // ========== 原有星星逻辑（完全不动） ==========
        int starCount = CollectableStar.CollectedCount;
        for (int i = 0; i < starSlots.Length; i++)
        {
            bool earned = i < starCount;
            starSlots[i].fullStar.enabled = earned;
            starSlots[i].emptyStar.enabled = !earned;
        }

        // ========== 🌟 新增：显示金币数量 ==========
        if (coinCountText != null)
        {
            coinCountText.text = "Coin: " + CollectableCoin.CollectedCount;
        }

        // ========== 保存本关最佳成绩到 PlayerPrefs ==========
        string currentLevel = SceneManager.GetActiveScene().name;
        LevelDataManager.SaveLevelResult(currentLevel, CollectableStar.CollectedCount, CollectableCoin.CollectedCount);

        // 重置统计
        CollectableStar.CollectedCount = 0;
        CollectableCoin.CollectedCount = 0; // 新增：清零金币
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
        CollectableCoin.CollectedCount = 0; // 新增：清零金币
    }

    [System.Serializable]
    public class StarSlot
    {
        public Image fullStar;
        public Image emptyStar;
    }
}