using UnityEngine;

/// <summary>
/// 场景 BGM 配置：每个场景放置一个，指定该场景的背景音乐。
/// 场景加载时自动通知 BGMManager 播放（带淡入淡出）。
///
/// 使用：
///   1. 在场景中创建空物体，挂载此脚本
///   2. 将 AudioClip 拖入 bgmClip 槽位
///   3. 运行时自动播放
///
/// 如果多个场景使用相同 clip，切换时不会中断播放。
/// </summary>
public class SceneBGM : MonoBehaviour
{
    [Header("场景 BGM")]
    [Tooltip("拖入此场景的背景音乐。留空则静音。")]
    public AudioClip bgmClip;

    [Header("播放设置")]
    [Tooltip("是否在场景加载时自动播放")]
    public bool playOnStart = true;

    [Tooltip("覆盖 BGMManager 的默认音量（-1 表示使用默认值）")]
    [Range(-1f, 1f)]
    public float volumeOverride = -1f;

    void Start()
    {
        if (!playOnStart) return;

        if (BGMManager.Instance != null)
        {
            // 如果有音量覆盖，先设置
            if (volumeOverride >= 0f)
                BGMManager.Instance.SetBGMVolume(volumeOverride);

            BGMManager.Instance.PlaySceneBGM(bgmClip);
        }
        else
        {
            Debug.LogWarning($"[SceneBGM] {gameObject.scene.name}: BGMManager 未找到，请确保首个场景包含 BGMManager。");
        }
    }
}
