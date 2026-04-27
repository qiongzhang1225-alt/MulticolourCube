using UnityEngine;
using System.Collections;

/// <summary>
/// 全局 BGM 管理器（跨场景单例）。
///
/// 功能：
///   1. 场景 BGM 播放，支持淡入淡出切换
///   2. 胜利 BGM 独立通道，不受 TimeScale 影响
///   3. 全局音量控制
///
/// 使用：
///   首个场景放置 BGMManager 预制体即可，后续场景自动保留。
///   每个场景放置 SceneBGM 组件指定该场景的背景音乐。
///   胜利时由 VictoryUI 调用 PlayVictoryBGM()。
/// </summary>
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("音量设置")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float victoryVolume = 0.6f;

    [Header("过渡设置")]
    public float crossFadeDuration = 1.0f;

    // ── 双 AudioSource 交叉淡入淡出 ──
    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;

    // ── 胜利 BGM 独立通道 ──
    private AudioSource victorySource;

    // ── 死亡 BGM 独立通道（如 waiting.wav）──
    private AudioSource deathSource;
    private Coroutine deathFadeRoutine;

    // ── 场景 BGM 暂停状态（死亡时暂停，复活时恢复）──
    private bool sceneBGMPaused = false;

    // ── 当前播放的 Clip（避免重复切换）──
    private AudioClip currentSceneClip;

    void Awake()
    {
        // 单例 + 跨场景保留
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 创建四个 AudioSource
        sourceA = CreateAudioSource("BGM_A");
        sourceB = CreateAudioSource("BGM_B");
        victorySource = CreateAudioSource("BGM_Victory");
        deathSource  = CreateAudioSource("BGM_Death");

        // 胜利/死亡通道不受 TimeScale 影响
        victorySource.ignoreListenerPause = true;
        deathSource.ignoreListenerPause = true;
        deathSource.loop = true; // 死亡面板停留期间循环

        activeSource = sourceA;
    }

    private AudioSource CreateAudioSource(string label)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.volume = 0f;
        return src;
    }

    // ══════════════════════════════════════
    //  场景 BGM
    // ══════════════════════════════════════

    /// <summary>
    /// 播放场景 BGM（带淡入淡出）。
    /// 如果与当前播放的相同 clip 则忽略。
    /// </summary>
    public void PlaySceneBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (clip == currentSceneClip && activeSource.isPlaying) return;

        currentSceneClip = clip;

        // 停止胜利 BGM（如果正在播放）
        StopVictoryBGM();

        StartCoroutine(CrossFade(clip));
    }

    /// <summary>停止场景 BGM（淡出）。</summary>
    public void StopSceneBGM()
    {
        if (activeSource.isPlaying)
            StartCoroutine(FadeOut(activeSource, crossFadeDuration));
        currentSceneClip = null;
    }

    private IEnumerator CrossFade(AudioClip newClip)
    {
        var oldSource = activeSource;
        var newSource = (activeSource == sourceA) ? sourceB : sourceA;
        activeSource = newSource;

        // 新源开始播放
        newSource.clip = newClip;
        newSource.volume = 0f;
        newSource.Play();

        // 交叉淡入淡出
        float elapsed = 0f;
        float oldStartVol = oldSource.volume;

        while (elapsed < crossFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / crossFadeDuration;

            newSource.volume = Mathf.Lerp(0f, bgmVolume, t);
            oldSource.volume = Mathf.Lerp(oldStartVol, 0f, t);

            yield return null;
        }

        newSource.volume = bgmVolume;
        oldSource.volume = 0f;
        oldSource.Stop();
    }

    // ══════════════════════════════════════
    //  胜利 BGM
    // ══════════════════════════════════════

    /// <summary>
    /// 播放胜利 BGM（淡出场景 BGM，淡入胜利 BGM）。
    /// 使用 unscaledDeltaTime，Time.timeScale=0 时也能播放。
    /// </summary>
    public void PlayVictoryBGM(AudioClip clip)
    {
        if (clip == null) return;

        // 淡出场景 BGM
        StartCoroutine(FadeOut(activeSource, crossFadeDuration * 0.5f));

        // 播放胜利 BGM
        victorySource.clip = clip;
        victorySource.loop = false;   // 胜利 BGM 通常只播一次
        victorySource.volume = 0f;
        victorySource.Play();
        StartCoroutine(FadeIn(victorySource, victoryVolume, crossFadeDuration * 0.5f));
    }

    /// <summary>停止胜利 BGM 并恢复场景 BGM。</summary>
    public void StopVictoryBGM()
    {
        if (victorySource.isPlaying)
        {
            victorySource.Stop();
            victorySource.volume = 0f;
        }
    }

    // ══════════════════════════════════════
    //  死亡 BGM（waiting.wav 等）
    // ══════════════════════════════════════

    /// <summary>
    /// 播放死亡 BGM（短暂等待音乐）。
    /// 调用前应先 PauseSceneBGM()，避免与场景 BGM 重叠。
    /// </summary>
    public void PlayDeathBGM(AudioClip clip, float fadeIn = 0.2f)
    {
        if (clip == null) return;

        if (deathFadeRoutine != null) StopCoroutine(deathFadeRoutine);
        deathSource.Stop();
        deathSource.clip = clip;
        deathSource.volume = 0f;
        deathSource.Play();
        deathFadeRoutine = StartCoroutine(FadeIn(deathSource, bgmVolume * 0.7f, fadeIn));
    }

    /// <summary>停止死亡 BGM（淡出）。</summary>
    public void StopDeathBGM(float fadeOut = 0.2f)
    {
        if (deathFadeRoutine != null) StopCoroutine(deathFadeRoutine);
        if (deathSource.isPlaying)
            deathFadeRoutine = StartCoroutine(FadeOut(deathSource, fadeOut));
    }

    // ══════════════════════════════════════
    //  场景 BGM 暂停 / 恢复
    // ══════════════════════════════════════

    /// <summary>暂停当前播放的场景 BGM（保留播放进度，可由 ResumeSceneBGM 恢复）。</summary>
    public void PauseSceneBGM()
    {
        if (activeSource != null && activeSource.isPlaying)
        {
            activeSource.Pause();
            sceneBGMPaused = true;
        }
    }

    /// <summary>恢复之前暂停的场景 BGM（从原进度续播）。</summary>
    public void ResumeSceneBGM()
    {
        if (sceneBGMPaused && activeSource != null)
        {
            activeSource.UnPause();
            sceneBGMPaused = false;
        }
    }

    // ══════════════════════════════════════
    //  音量控制
    // ══════════════════════════════════════

    /// <summary>设置 BGM 主音量（0~1）。</summary>
    public void SetBGMVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (activeSource.isPlaying)
            activeSource.volume = bgmVolume;
    }

    /// <summary>设置胜利 BGM 音量（0~1）。</summary>
    public void SetVictoryVolume(float vol)
    {
        victoryVolume = Mathf.Clamp01(vol);
        if (victorySource.isPlaying)
            victorySource.volume = victoryVolume;
    }

    // ══════════════════════════════════════
    //  工具
    // ══════════════════════════════════════

    private IEnumerator FadeIn(AudioSource src, float targetVol, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(0f, targetVol, elapsed / duration);
            yield return null;
        }
        src.volume = targetVol;
    }

    private IEnumerator FadeOut(AudioSource src, float duration)
    {
        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        src.volume = 0f;
        src.Stop();
    }
}
