using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Boss 血条：屏幕顶部一条横向血条 + 数字显示。
/// 由 BossTriangle 在 Start 中调用 SetMaxHP / SetHP 更新。
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI 引用")]
    public Image fillImage;                 // Image (Type=Filled, FillMethod=Horizontal)
    public TextMeshProUGUI hpText;          // "10 / 10"
    public CanvasGroup group;               // 整体显隐

    [Header("颜色")]
    public Color fullColor = new Color(0.3f, 1f, 0.4f);
    public Color lowColor  = new Color(1f, 0.3f, 0.3f);

    private int maxHP = 10;
    private int currentHP = 10;

    void Start()
    {
        if (group != null) group.alpha = 1f;
        UpdateVisual();
    }

    public void SetMaxHP(int v)
    {
        maxHP = Mathf.Max(1, v);
        currentHP = maxHP;
        UpdateVisual();
    }

    public void SetHP(int v)
    {
        currentHP = Mathf.Clamp(v, 0, maxHP);
        UpdateVisual();
        if (currentHP <= 0 && group != null)
            group.alpha = 0f;
    }

    private void UpdateVisual()
    {
        float ratio = (float)currentHP / maxHP;
        if (fillImage != null)
        {
            fillImage.fillAmount = ratio;
            fillImage.color = Color.Lerp(lowColor, fullColor, ratio);
        }
        if (hpText != null)
            hpText.text = currentHP + " / " + maxHP;
    }
}
