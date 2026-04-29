using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把挂载 Canvas 的 CanvasScaler 自动设置为「Scale With Screen Size」模式，
/// 确保 UI 在任何分辨率下都等比缩放，而不是固定像素大小导致高分辨率下偏小/低分辨率下溢出。
///
/// 用法：把本组件挂到场景里每个 Canvas 根节点上即可。
/// 若 Canvas 上还没有 CanvasScaler，会自动添加。
///
/// 推荐参数（可在 Inspector 调整）：
///   Reference Resolution : 1920 × 1080
///   Screen Match Mode    : Match Width Or Height (0.5 = 等比)
/// </summary>
[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public class CanvasScalerAutoConfig : MonoBehaviour
{
    [Header("参考分辨率（设计稿分辨率，建议 1920×1080）")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("宽高匹配权重（0=按宽缩放  1=按高缩放  0.5=取中）")]
    [Range(0f, 1f)]
    public float matchWidthOrHeight = 0.5f;

    void Awake()
    {
        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode             = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution     = referenceResolution;
        scaler.screenMatchMode         = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight      = matchWidthOrHeight;
        scaler.referencePixelsPerUnit  = 100f;
    }
}
