using UnityEngine;
using System;

/// <summary>
/// 方块颜色枚举 —— 直接对应玩家四个面的颜色名称。
/// 运行时自动从 PlayerColorSensor 读取精确颜色值。
/// </summary>
public enum CubeFace
{
    Yellow,  // 黄色（顶面 faceUp）
    Blue,    // 蓝色（底面 faceDown）
    Red,     // 红色（左面 faceLeft）
    Green,   // 绿色（右面 faceRight）
}

/// <summary>
/// 可变色地块：接触玩家后转换为玩家面颜色。
///
/// 功能：
///   1. 颜色储存 —— revertColorOnLeave = false 时，玩家离开后颜色保留。
///   2. 颜色条件 —— 选择 requiredFace（枚举下拉框），运行时自动匹配玩家对应面的精确颜色。
///   3. 串联触发 —— 多个地块挂载同一个 ColorConditionGroup，
///      所有地块颜色都满足各自要求时，条件组触发（等效按钮激活）。
/// </summary>
public class ColorChangePlatform : MonoBehaviour, IResettable
{
    [Header("基础行为")]
    [Tooltip("玩家离开后是否恢复原色（false = 储存颜色）")]
    public bool revertColorOnLeave = true;

    [Header("颜色条件（可选）")]
    [Tooltip("启用后，当地块颜色与指定面颜色匹配时视为已激活")]
    public bool useColorCondition = false;

    [Tooltip("此地块需要变成什么颜色才算激活（运行时自动读取精确颜色值）")]
    public CubeFace requiredFace = CubeFace.Blue;

    [Tooltip("颜色比较容差")]
    [Range(0.01f, 0.3f)]
    public float colorTolerance = 0.15f;

    [Header("条件组（可选）")]
    [Tooltip("所属的颜色条件组，多个地块共用同一组实现串联 AND 逻辑")]
    public ColorConditionGroup conditionGroup;

    [Header("激活反馈（可选）")]
    [Tooltip("颜色匹配后激活的指示物体（如发光边框 / 粒子）")]
    public GameObject matchIndicator;

    // ── 内部状态 ──
    private Color _originalColor;
    private Color _currentColor;
    private Color _resolvedRequiredColor;   // 运行时从玩家读取的精确颜色
    private Renderer _renderer;
    private bool _isColorMatched = false;

    /// <summary>当前实际颜色。</summary>
    public Color CurrentColor => _currentColor;

    /// <summary>运行时解析后的目标颜色。</summary>
    public Color ResolvedRequiredColor => _resolvedRequiredColor;

    /// <summary>当前颜色是否满足条件。</summary>
    public bool IsColorMatched => _isColorMatched;

    /// <summary>颜色发生变化时通知（参数为自身引用）。</summary>
    public event Action<ColorChangePlatform> OnColorChanged;

    // ──────────────────────────────────────
    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
            _currentColor = _originalColor;
        }

        if (matchIndicator != null)
            matchIndicator.SetActive(false);
    }

    void Start()
    {
        // ── 从玩家读取精确面颜色 ──
        if (useColorCondition)
            ResolveRequiredColor();

        // 自动向条件组注册
        if (conditionGroup != null)
            conditionGroup.RegisterPlatform(this);
    }

    /// <summary>
    /// 从场景中的 PlayerColorSensor 读取对应面的精确颜色值。
    /// 彻底解决手动拾色器颜色不匹配的问题。
    /// </summary>
    private void ResolveRequiredColor()
    {
        var sensor = FindObjectOfType<PlayerColorSensor>();
        if (sensor != null)
        {
            switch (requiredFace)
            {
                case CubeFace.Yellow: _resolvedRequiredColor = sensor.faceUp;    break;
                case CubeFace.Blue:   _resolvedRequiredColor = sensor.faceDown;  break;
                case CubeFace.Red:    _resolvedRequiredColor = sensor.faceLeft;  break;
                case CubeFace.Green:  _resolvedRequiredColor = sensor.faceRight; break;
            }
            Debug.Log($"[ColorPlatform] {gameObject.name}: 需要={requiredFace} → " +
                $"精确颜色=({_resolvedRequiredColor.r:F3},{_resolvedRequiredColor.g:F3},{_resolvedRequiredColor.b:F3})");
        }
        else
        {
            // 找不到玩家时使用标准颜色作为回退
            switch (requiredFace)
            {
                case CubeFace.Yellow: _resolvedRequiredColor = Color.yellow; break;
                case CubeFace.Blue:   _resolvedRequiredColor = Color.blue;   break;
                case CubeFace.Red:    _resolvedRequiredColor = Color.red;    break;
                case CubeFace.Green:  _resolvedRequiredColor = Color.green;  break;
            }
            Debug.LogWarning($"[ColorPlatform] {gameObject.name}: 未找到 PlayerColorSensor，使用默认颜色");
        }
    }

    // ──────── 碰撞：变色（接触玩家面旋转 / 携色球） ────────

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 玩家方块：根据接触面颜色变色
        if (collision.collider.CompareTag("Player"))
        {
            PlayerColorSensor sensor = collision.collider.GetComponent<PlayerColorSensor>();
            if (sensor != null)
            {
                Color contactColor = sensor.GetContactFaceColor(collision);
                ApplyColor(contactColor);
            }
            return;
        }

        // 携色球：直接使用球携带的颜色
        BallColorCarrier carrier = collision.collider.GetComponent<BallColorCarrier>();
        if (carrier != null)
        {
            ApplyColor(carrier.CarriedColor);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (revertColorOnLeave)
        {
            if (collision.collider.CompareTag("Player") ||
                collision.collider.GetComponent<BallColorCarrier>() != null)
            {
                ApplyColor(_originalColor);
            }
        }
    }

    // ──────── 颜色应用 & 条件检测 ────────

    /// <summary>应用新颜色并更新条件状态。</summary>
    private void ApplyColor(Color color)
    {
        _currentColor = color;
        if (_renderer != null)
            _renderer.material.color = color;

        // 条件检测（使用运行时解析的精确颜色）
        if (useColorCondition)
        {
            bool wasMatched = _isColorMatched;
            _isColorMatched = ColorsApproximatelyEqual(_currentColor, _resolvedRequiredColor, colorTolerance);

            // 激活反馈
            if (matchIndicator != null)
                matchIndicator.SetActive(_isColorMatched);

            // 状态变化时通知条件组
            if (_isColorMatched != wasMatched)
            {
                Debug.Log($"[ColorPlatform] {gameObject.name} 颜色条件 " +
                    (_isColorMatched ? "✓ 满足" : "✗ 未满足") +
                    $" (当前={_currentColor}, 目标={_resolvedRequiredColor})");
                OnColorChanged?.Invoke(this);
            }
        }
    }

    // ──────── 工具方法 ────────

    /// <summary>近似颜色比较（忽略 Alpha）。</summary>
    private static bool ColorsApproximatelyEqual(Color a, Color b, float tolerance)
    {
        return Mathf.Abs(a.r - b.r) < tolerance
            && Mathf.Abs(a.g - b.g) < tolerance
            && Mathf.Abs(a.b - b.b) < tolerance;
    }

    /// <summary>外部手动设置颜色（如编辑器工具）。</summary>
    public void SetColor(Color color)
    {
        ApplyColor(color);
    }

    /// <summary>
    /// 运行时切换"需要的颜色"（拼图谜题刷新等场景）。
    /// 会重新解析精确颜色 + 立即重新评估当前 IsColorMatched 状态。
    /// </summary>
    public void SetRequiredFace(CubeFace face)
    {
        requiredFace = face;
        useColorCondition = true;
        ResolveRequiredColor();

        // 重新评估当前匹配状态，必要时通知监听者
        bool wasMatched = _isColorMatched;
        _isColorMatched = ColorsApproximatelyEqual(_currentColor, _resolvedRequiredColor, colorTolerance);
        if (matchIndicator != null) matchIndicator.SetActive(_isColorMatched);
        if (_isColorMatched != wasMatched)
            OnColorChanged?.Invoke(this);
    }

    /// <summary>重置到原始颜色（检查点恢复时调用）。</summary>
    public void ResetPlatformColor()
    {
        ApplyColor(_originalColor);
    }

    // ── IResettable ──
    private Color _savedColor;
    private bool _savedMatched;

    public void SaveCheckpointState()
    {
        _savedColor = _currentColor;
        _savedMatched = _isColorMatched;
    }

    public void ResetToCheckpointState()
    {
        _isColorMatched = _savedMatched;
        ApplyColor(_savedColor);
    }
}