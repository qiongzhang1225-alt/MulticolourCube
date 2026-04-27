using UnityEngine;

/// <summary>
/// 携色球：让非玩家物体（如小球）也能触发 ColorChangePlatform 变色。
/// Inspector 中选择四种颜色之一（黄/蓝/红/绿），
/// 运行时自动从 PlayerColorSensor 读取精确颜色值，
/// 并同步更新自身 SpriteRenderer 显示色。
/// </summary>
public class BallColorCarrier : MonoBehaviour
{
    [Header("携带颜色")]
    [Tooltip("选择此球携带的颜色（与玩家四面颜色对应）")]
    public CubeFace ballFace = CubeFace.Blue;

    // ── 运行时解析后的精确颜色 ──
    private Color _resolvedColor;
    private SpriteRenderer _sr;

    /// <summary>当前携带的精确颜色（运行时从 PlayerColorSensor 读取）。</summary>
    public Color CarriedColor => _resolvedColor;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        ResolveColor();

        // 同步 SpriteRenderer 显示色
        if (_sr != null)
            _sr.color = _resolvedColor;
    }

    /// <summary>运行时切换携带颜色（生成谜题小球时使用）。</summary>
    public void SetFace(CubeFace face)
    {
        ballFace = face;
        ResolveColor();
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _sr.color = _resolvedColor;
    }

    /// <summary>
    /// 从场景中的 PlayerColorSensor 读取对应面的精确颜色值，
    /// 与 ColorChangePlatform 保持一致，避免手动拾色导致的误差。
    /// </summary>
    private void ResolveColor()
    {
        var sensor = FindObjectOfType<PlayerColorSensor>();
        if (sensor != null)
        {
            switch (ballFace)
            {
                case CubeFace.Yellow: _resolvedColor = sensor.faceUp;    break;
                case CubeFace.Blue:   _resolvedColor = sensor.faceDown;  break;
                case CubeFace.Red:    _resolvedColor = sensor.faceLeft;  break;
                case CubeFace.Green:  _resolvedColor = sensor.faceRight; break;
            }
        }
        else
        {
            // 回退：使用标准颜色
            switch (ballFace)
            {
                case CubeFace.Yellow: _resolvedColor = Color.yellow; break;
                case CubeFace.Blue:   _resolvedColor = Color.blue;   break;
                case CubeFace.Red:    _resolvedColor = Color.red;    break;
                case CubeFace.Green:  _resolvedColor = Color.green;  break;
            }
            Debug.LogWarning($"[BallColorCarrier] {gameObject.name}: 未找到 PlayerColorSensor，使用默认颜色");
        }
    }
}
