using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 局内信息 HUD：实时显示星星、金币、时间。
///
/// 布局：
///   左上角 —— ★ 星星数   ● 金币数
///   正上方 —— 计时器
///
/// 使用：
///   GameHUD 预制体自带独立 Canvas（sortOrder=100），
///   直接放在场景根层级即可，不依赖场景内任何 Canvas。
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    [Header("UI 引用")]
    public TextMeshProUGUI starText;     // 星星数量文本
    public TextMeshProUGUI coinText;     // 金币数量文本
    public TextMeshProUGUI timerText;    // 计时器文本
    public Image starIcon;               // 星星图标（可选）
    public Image coinIcon;               // 金币图标（可选）

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // ── 更新星星 ──
        if (starText != null)
        {
            starText.text = "× " + CollectableStar.CollectedCount;
        }

        // ── 更新金币 ──
        if (coinText != null)
        {
            coinText.text = "× " + CollectableCoin.CollectedCount;
        }

        // ── 更新时间 ──
        if (timerText != null && GameTimer.Instance != null)
        {
            timerText.text = GameTimer.Instance.GetFormattedTime();
        }
    }
}
