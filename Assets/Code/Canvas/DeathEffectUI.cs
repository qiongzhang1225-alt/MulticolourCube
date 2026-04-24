using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// 死亡特效 UI：黑幕淡入 → 显示死亡次数 → 淡出复活。
///
/// 注意：DeathOverlay 预制体自带独立 Canvas（sortOrder=999），
/// 不依赖场景内任何 Canvas，直接放在场景根层级即可。
/// </summary>
public class DeathEffectUI : MonoBehaviour
{
    public static DeathEffectUI Instance { get; private set; }

    [Header("UI 引用")]
    public CanvasGroup overlayGroup;          // 黑幕 CanvasGroup（控制整体透明度）
    public TextMeshProUGUI deathCountText;    // 显示 "× N"
    public TextMeshProUGUI tipText;           // 底部提示文字（可选）

    [Header("动画参数")]
    public float fadeInDuration  = 0.35f;     // 黑幕淡入时长
    public float holdDuration    = 0.8f;      // 黑幕停留时长（显示文字）
    public float fadeOutDuration = 0.45f;     // 黑幕淡出时长
    public float textPunchScale  = 1.3f;      // 文字弹出放大倍数

    [Header("死亡特效预留（后续可扩展）")]
    [Tooltip("死亡时播放的粒子特效（在玩家位置生成）")]
    public ParticleSystem deathParticlePrefab;

    [Tooltip("复活时播放的粒子特效（在复活点位置生成）")]
    public ParticleSystem respawnParticlePrefab;

    [Tooltip("死亡音效")]
    public AudioClip deathSFX;

    [Tooltip("复活音效")]
    public AudioClip respawnSFX;

    // 死亡统计
    public static int DeathCount { get; private set; } = 0;

    // ──────── 死亡特效事件（外部订阅，后续扩展用）────────
    /// <summary>死亡特效开始时触发（参数：玩家死亡位置）。</summary>
    public static event Action<Vector3> OnDeathEffectStart;

    /// <summary>复活淡出开始时触发（参数：复活位置）。</summary>
    public static event Action<Vector3> OnRespawnEffectStart;

    /// <summary>整个死亡→复活流程结束后触发。</summary>
    public static event Action OnDeathSequenceComplete;

    // 缓存的死亡/复活位置（供特效使用）
    private Vector3 _lastDeathPosition;
    private Vector3 _lastRespawnPosition;

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
        // 单例（场景切换时自动更新）
        Instance = this;

        if (overlayGroup != null)
        {
            // 用 CanvasGroup 控制显隐，不用 SetActive（避免禁用脚本导致 StartCoroutine 失效）
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
        }
    }

    /// <summary>场景加载时重置死亡计数。</summary>
    public static void ResetDeathCount()
    {
        DeathCount = 0;
    }

    // ──────── 主流程 ────────

    /// <summary>
    /// 播放完整死亡特效序列，返回协程供 PlayerRespawn 等待。
    /// 用法：yield return StartCoroutine(DeathEffectUI.Instance.PlayDeathSequence(playerPos));
    /// </summary>
    /// <param name="deathPosition">玩家死亡时的世界坐标（用于生成特效）</param>
    public IEnumerator PlayDeathSequence(Vector3 deathPosition)
    {
        DeathCount++;
        _lastDeathPosition = deathPosition;
        Debug.Log("[DeathEffect] × " + DeathCount);

        // ── 触发死亡特效事件 ──
        OnDeathEffectStart?.Invoke(deathPosition);
        PlayDeathVFX(deathPosition);

        // 激活黑幕（通过 CanvasGroup 控制）
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = true;
        overlayGroup.blocksRaycasts = true;

        // 隐藏文字，等黑幕完全覆盖后再显示
        if (deathCountText != null) deathCountText.alpha = 0f;
        if (tipText != null) tipText.alpha = 0f;

        // ── 阶段 1：黑幕淡入 ──
        yield return FadeOverlay(0f, 1f, fadeInDuration);

        // ── 阶段 2：显示死亡次数文字（弹出动画）──
        if (deathCountText != null)
        {
            deathCountText.text = "× " + DeathCount;
            yield return PunchText(deathCountText, 0.3f);
        }

        // 显示随机提示
        if (tipText != null)
        {
            tipText.text = tips[UnityEngine.Random.Range(0, tips.Length)];
            yield return FadeText(tipText, 0f, 1f, 0.2f);
        }

        // ── 阶段 3：停留 ──
        yield return new WaitForSecondsRealtime(holdDuration);
    }

    /// <summary>
    /// 无参版本（向后兼容）。
    /// </summary>
    public IEnumerator PlayDeathSequence()
    {
        yield return PlayDeathSequence(Vector3.zero);
    }

    /// <summary>
    /// 复活后淡出黑幕。
    /// 用法：yield return StartCoroutine(DeathEffectUI.Instance.PlayRespawnFadeOut(respawnPos));
    /// </summary>
    public IEnumerator PlayRespawnFadeOut(Vector3 respawnPosition)
    {
        _lastRespawnPosition = respawnPosition;

        // ── 触发复活特效事件 ──
        OnRespawnEffectStart?.Invoke(respawnPosition);
        PlayRespawnVFX(respawnPosition);

        // 先隐藏文字
        if (deathCountText != null)
            yield return FadeText(deathCountText, 1f, 0f, 0.15f);
        if (tipText != null) tipText.alpha = 0f;

        // 黑幕淡出
        yield return FadeOverlay(1f, 0f, fadeOutDuration);

        // 完全隐藏
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        // 通知流程完成
        OnDeathSequenceComplete?.Invoke();
    }

    /// <summary>无参版本（向后兼容）。</summary>
    public IEnumerator PlayRespawnFadeOut()
    {
        yield return PlayRespawnFadeOut(Vector3.zero);
    }

    // ──────── 死亡特效（预留，后续填入具体实现）────────

    /// <summary>
    /// 在死亡位置播放视觉特效。
    /// 后续可在此添加粒子、屏幕震动等效果。
    /// </summary>
    private void PlayDeathVFX(Vector3 position)
    {
        // ── 粒子特效 ──
        if (deathParticlePrefab != null)
        {
            var ps = Instantiate(deathParticlePrefab, position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }

        // ── 音效 ──
        if (deathSFX != null)
        {
            AudioSource.PlayClipAtPoint(deathSFX, position);
        }

        // ── TODO: 在这里添加更多死亡特效 ──
        // 例如：屏幕震动、闪白、慢动作、碎片飞散等
        // CameraShake.Instance?.Shake(0.3f, 0.2f);
        // PostProcessing.Instance?.FlashWhite(0.1f);
    }

    /// <summary>
    /// 在复活位置播放视觉特效。
    /// 后续可在此添加粒子、光圈等效果。
    /// </summary>
    private void PlayRespawnVFX(Vector3 position)
    {
        // ── 粒子特效 ──
        if (respawnParticlePrefab != null)
        {
            var ps = Instantiate(respawnParticlePrefab, position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }

        // ── 音效 ──
        if (respawnSFX != null)
        {
            AudioSource.PlayClipAtPoint(respawnSFX, position);
        }

        // ── TODO: 在这里添加更多复活特效 ──
        // 例如：光圈扩散、粒子汇聚、无敌闪光等
    }

    // ──────── 动画工具（使用 unscaledDeltaTime，暂停时也能播放）────────

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

    /// <summary>文字从大到正常的弹出效果（EaseOutBack）。</summary>
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
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            rt.localScale = Vector3.Lerp(big, original, ease);
            yield return null;
        }
        rt.localScale = original;
    }
}
