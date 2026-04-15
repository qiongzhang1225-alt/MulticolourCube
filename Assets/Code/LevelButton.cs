using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 关卡选择按钮数据组件。
/// 挂在 LevelSelect 场景中每个关卡按钮 GameObject 上，由 LevelSelect 读取并配置状态。
/// </summary>
public class LevelButton : MonoBehaviour
{
    [Header("关卡信息")]
    public string levelName;               // 对应场景名，如 "Level 1"

    [Header("UI 引用")]
    public Button button;                  // 按钮组件
    public TextMeshProUGUI coinText;       // 显示最佳金币数
    public GameObject lockOverlay;         // 锁定遮罩（未解锁时显示）

    [Header("星星槽位（参考胜利UI，3个槽）")]
    public StarSlot[] starSlots;           // 长度建议为 3，与每关最多 3 颗星对应

    [System.Serializable]
    public class StarSlot
    {
        public Image fullStar;   // 已获得时显示
        public Image emptyStar;  // 未获得时显示
    }

    /// <summary>
    /// 根据最佳星星数刷新星星槽位显示（由 LevelSelect 调用）。
    /// </summary>
    public void RefreshStars(int bestStars)
    {
        if (starSlots == null) return;
        for (int i = 0; i < starSlots.Length; i++)
        {
            bool earned = i < bestStars;
            if (starSlots[i].fullStar  != null) starSlots[i].fullStar.enabled  = earned;
            if (starSlots[i].emptyStar != null) starSlots[i].emptyStar.enabled = !earned;
        }
    }
}
