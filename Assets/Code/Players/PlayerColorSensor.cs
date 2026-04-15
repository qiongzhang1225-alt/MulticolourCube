using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PlayerColorSensor : FaceAbilityBase
{
    [Header("方块自身四面颜色（旋转不影响）")]
    [Tooltip("方块自身顶面的颜色")] public Color faceUp = Color.yellow;
    [Tooltip("方块自身底面的颜色")] public Color faceDown = Color.blue;
    [Tooltip("方块自身左面的颜色")] public Color faceLeft = Color.red;
    [Tooltip("方块自身右面的颜色")] public Color faceRight = Color.green;

    [Header("传感器设置")]
    [Tooltip("方向判断阈值（0.6是通用值，越小越宽松）")]
    [Range(0.1f, 0.9f)] public float normalThreshold = 0.6f;
    [Tooltip("是否在碰撞时自动记录颜色")]
    public bool autoRecordColorOnCollision = true;

    // 内部状态
    private Collider2D col;
    private Color lastContactedColor; // 记录最后一次接触的颜色
    private bool hasValidContact;     // 是否有有效接触

    // 公共只读属性：外部可以直接获取最后一次接触的颜色
    public Color LastContactedColor => lastContactedColor;
    public bool HasValidContact => hasValidContact;

    protected override void Awake()
    {
        base.Awake();

        // 获取碰撞体（颜色传感器需要物理碰撞来获取法线）
        col = GetComponent<Collider2D>();
        col.isTrigger = false; // 强制设为非Trigger，才能获取Collision2D

        if (blockController == null)
        {
            Debug.LogError($"【颜色传感器错误】{gameObject.name} 找不到 BlockController！", this);
            enabled = false;
        }

        // 初始化
        lastContactedColor = Color.clear;
        hasValidContact = false;
    }

    // --- 基类生命周期 ---
    protected override void OnAbilityEnable()
    {
        // 面激活时，重置接触状态
        hasValidContact = false;
        lastContactedColor = Color.clear;
    }

    protected override void OnAbilityDisable()
    {
        // 面失活时，保持最后一次记录的颜色（可选）
    }

    // --- 核心逻辑：物理碰撞检测颜色 ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 只有面激活时才工作
        if (!isFaceActive) return;

        // 自动记录颜色
        if (autoRecordColorOnCollision)
        {
            lastContactedColor = GetContactFaceColor(collision);
            hasValidContact = true;
            Debug.Log($"【颜色传感器】检测到接触颜色：{lastContactedColor}", this);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 持续接触时，也可以更新颜色（可选，根据需求开启）
        // if (isFaceActive && autoRecordColorOnCollision)
        // {
        //     lastContactedColor = GetContactFaceColor(collision);
        // }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // 离开碰撞时，重置接触状态
        hasValidContact = false;
    }

    // --- 【完全保留你的原始代码逻辑】核心颜色检测方法 ---
    /// <summary>
    /// 无论方块怎么旋转，返回当前接触地块的面的颜色
    /// </summary>
    public Color GetContactFaceColor(Collision2D collision)
    {
        // 1. 获取碰撞的世界法线
        Vector2 worldNormal = collision.contacts[0].normal;

        // 2. 【关键】转换为方块本地坐标系的法线，完美适配旋转
        Vector2 localNormal = transform.InverseTransformDirection(worldNormal);

        // 3. 归一化方向，消除误差
        localNormal.Normalize();

        // 4. 判断：方块哪个本地面接触了地块（和旋转完全无关）
        if (localNormal.y > normalThreshold) return faceDown;   // 方块底面触地
        if (localNormal.y < -normalThreshold) return faceUp;     // 方块顶面触地
        if (localNormal.x > normalThreshold) return faceLeft;   // 方块左面触地
        if (localNormal.x < -normalThreshold) return faceRight;  // 方块右面触地

        // 默认兜底
        return faceDown;
    }

    // --- 公共工具方法：手动重置传感器 ---
    public void ResetSensor()
    {
        lastContactedColor = Color.clear;
        hasValidContact = false;
    }

    // --- 安全兜底 ---
    private void OnDisable()
    {
        // 面失活时，保持最后一次记录的颜色
    }
}