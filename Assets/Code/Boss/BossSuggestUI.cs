using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss 关卡 — 提示面板（世界空间 SpriteRenderer 版）：
///   8 个子物体（一般为 6 方 + 2 圆），每个挂 SpriteRenderer。
///   颜色由 BossArenaController 通过 SetColors(Color[]) 推入；
///   也可单独通过 SetCubeColor(int idx, Color c) 改某一个。
///
/// 推荐顺序（与 BossArenaController 索引一致）：
///   [0..3] = 左区 4 块（其中索引 0 = 不可达 → 圆形）
///   [4..7] = 右区 4 块（其中索引 4 = 不可达 → 圆形）
///
/// Inspector：
///   - cubeRenderers: 长度 = 8，按上面顺序拖入子物体的 SpriteRenderer
///   - timerRenderer（可选）: 倒计时进度条 SpriteRenderer（用 localScale 表示）
///   - timerImage（可选）: 若用 UI 模式 Filled Image 做倒计时，可填这个
/// </summary>
public class BossSuggestUI : MonoBehaviour
{
    [Header("8 个方块/圆的 SpriteRenderer")]
    [Tooltip("按从左到右顺序拖入：左区 4 个（圆 + 3 方）+ 右区 4 个（圆 + 3 方）")]
    public SpriteRenderer[] cubeRenderers;

    [Header("倒计时（任选其一或都不填）")]
    [Tooltip("世界空间倒计时进度条，按 X 轴 localScale 缩短表示剩余比例")]
    public Transform timerRenderer;
    [Tooltip("UI Image 倒计时（FillMethod=Horizontal）")]
    public Image timerImage;

    [Header("视觉")]
    [Tooltip("解出后整体闪烁的颜色")] public Color solvedFlashColor = Color.white;
    [Tooltip("未初始化时的默认颜色（首回合开始前显示的颜色）")]
    public Color idleColor = Color.white;
    [Tooltip("启动时是否把所有方块刷成 idleColor。关闭则保留 prefab/场景里设置的颜色")]
    public bool paintIdleOnAwake = false;

    private float timerInitialScaleX = 1f;

    void Awake()
    {
        if (timerRenderer != null) timerInitialScaleX = timerRenderer.localScale.x;

        if (paintIdleOnAwake && cubeRenderers != null)
        {
            foreach (var r in cubeRenderers) if (r != null) r.color = idleColor;
        }
    }

    /// <summary>批量设置所有方块颜色。长度不一致时取较小者。</summary>
    public void SetColors(Color[] colors)
    {
        if (cubeRenderers == null || colors == null) return;
        int n = Mathf.Min(cubeRenderers.Length, colors.Length);
        for (int i = 0; i < n; i++)
        {
            if (cubeRenderers[i] != null) cubeRenderers[i].color = colors[i];
        }
    }

    /// <summary>单独设置某一格颜色。</summary>
    public void SetCubeColor(int index, Color c)
    {
        if (cubeRenderers == null) return;
        if (index < 0 || index >= cubeRenderers.Length) return;
        if (cubeRenderers[index] != null) cubeRenderers[index].color = c;
    }

    /// <summary>0..1 倒计时填充。</summary>
    public void SetTimerFill(float ratio01)
    {
        ratio01 = Mathf.Clamp01(ratio01);
        if (timerImage != null) timerImage.fillAmount = ratio01;
        if (timerRenderer != null)
        {
            var s = timerRenderer.localScale;
            s.x = timerInitialScaleX * ratio01;
            timerRenderer.localScale = s;
        }
    }

    /// <summary>把所有方块染成"通关"颜色，配合解谜成功反馈。</summary>
    public void FlashSolved()
    {
        if (cubeRenderers == null) return;
        for (int i = 0; i < cubeRenderers.Length; i++)
            if (cubeRenderers[i] != null) cubeRenderers[i].color = solvedFlashColor;
    }
}
