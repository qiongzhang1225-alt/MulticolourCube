using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 死亡特效 UI：黑幕淡入 → 显示死亡次数 → 淡出复活。
/// 挂在 Canvas 下的 DeathOverlay 面板上。
/// </summary>
public class DeathEffectUI : MonoBehaviour
{
    public static DeathEffectUI Instance { get; private set; }

    [Header("UI 引用")]
    public CanvasGroup overlayGroup;          // 黑幕 CanvasGroup（控制整体透明度）
    public TextMeshProUGUI deathCountText;    // 显示 "第 X 次死亡"
    public TextMeshProUGUI tipText;           // 底部提示文字（可选）

    [Header("动画参数")]
    public float fadeInDuration  = 0.35f;     // 黑幕淡入时长
    public float holdDuration    = 0.8f;      // 黑幕停留时长（显示文字）
    public float fadeOutDuration = 0.45f;     // 黑幕淡出时长
    public float textPunchScale  = 1.3f;      // 文字弹出放大倍数

    // 死亡统计
    public static int DeathCount { get; private set; } = 0;

    // 提示语池（随机显示一条）
    private static readonly string[] tips = {
        "再试一次...",
        "别灰心！",
        "小心脚下！",
        "注意节奏！",
        "你可以的！",
        "冷静，再来！",
        "稳住...",
        "差一点点！",
    };

    void Awake()
    {
        Instance = this;
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>场景加载时重置死亡计数（由 SceneLoader 或自身 Start 调用）。</summary>
    public static void ResetDeathCount()
    {
        DeathCount = 0;
    }

    /// <summary>
    /// 播放完整死亡特效序列，返回协程供 PlayerRespawn 等待。
    /// 调用方式：yield return StartCoroutine(DeathEffectUI.Instance.PlayDeathSequence());
    /// </summary>
    public IEnumerator PlayDeathSequence()
    {
        DeathCount++;
        Debug.Log("[DeathEffect] × " + DeathCount);

        // 激活面板
        overlayGroup.gameObject.SetActive(true);
        overlayGroup.alpha = 0f;

        // 隐藏文字，等黑幕完全覆盖后再显示
        if (deathCountText != null) deathCountText.alpha = 0f;
        if (tipText != null) tipText.alpha = 0f;

        // ── 阶段 1：黑幕淡入 ──
        yield return StartCoroutine(FadeOverlay(0f, 1f, fadeInDuration));

        // ── 阶段 2：显示死亡次数文字（弹出动画）──
        if (deathCountText != null)
        {
            deathCountText.text = "× " + DeathCount;
            yield return StartCoroutine(PunchText(deathCountText, 0.3f));
        }

        // 显示随机提示
        if (tipText != null)
        {
            tipText.text = tips[Random.Range(0, tips.Length)];
            yield return StartCoroutine(FadeText(tipText, 0f, 1f, 0.2f));
        }

        // ── 阶段 3：停留 ──
        yield return new WaitForSecondsRealtime(holdDuration);
    }

    /// <summary>
    /// 复活后淡出黑幕（由 PlayerRespawn 在传送完成后调用）。
    /// </summary>
    public IEnumerator PlayRespawnFadeOut()
    {
        // 先隐藏文字
        if (deathCountText != null)
            yield return StartCoroutine(FadeText(deathCountText, 1f, 0f, 0.15f));
        if (tipText != null) tipText.alpha = 0f;

        // 黑幕淡出
        yield return StartCoroutine(FadeOverlay(1f, 0f, fadeOutDuration));

        overlayGroup.gameObject.SetActive(false);
    }

    // ──────── 动画工具 ────────

    IEnumerator FadeOverlay(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlayGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        overlayGroup.alpha = to;
    }

    IEnumerator FadeText(TextMeshProUGUI text, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            text.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        text.alpha = to;
    }

    /// <summary>文字从大到正常的弹出效果。</summary>
    IEnumerator PunchText(TextMeshProUGUI text, float duration)
    {
        var rt = text.rectTransform;
        Vector3 original = Vector3.one;
        Vector3 big = original * textPunchScale;
        float elapsed = 0f;

        text.alpha = 1f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // EaseOutBack 缓动
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            rt.localScale = Vector3.Lerp(big, original, ease);
            yield return null;
        }
        rt.localScale = original;
    }
}
