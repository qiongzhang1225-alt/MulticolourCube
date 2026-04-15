using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class JumpBoostAbility : FaceAbilityBase
{
    [Header("接触判定")]
    [Tooltip("检测哪些图层的物体（通常选 Ground）")]
    public LayerMask surfaceLayers;
    [Tooltip("是否只有当前面激活时才生效（建议保持true）")]
    public bool requireCorrectFace = true;

    [Header("倍率设置")]
    [Tooltip("跳跃力增强倍率（1.5 = 1.5倍）")]
    public float jumpBoostMultiplier = 1.5f;

    private Collider2D col;
    private int currentOverlapCount = 0; // 防止同时踩多个弹跳面时重复计算

    // 基线数值缓存
    private float originalGroundJumpForce;
    private float originalWallJumpForce;
    private bool isBoostApplied = false;

    protected override void Awake()
    {
        base.Awake(); // 必须先调用基类Awake获取controller

        // 自动设置碰撞体
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // 空值保护
        if (blockController == null)
        {
            Debug.LogError($"【弹跳面错误】{gameObject.name} 找不到父物体的 BlockController！", this);
            enabled = false;
        }
    }

    // --- 1. 基类生命周期：面激活/失活时的处理 ---
    protected override void OnAbilityEnable()
    {
        // 当面被激活时，如果已经碰着弹跳面，直接应用增强
        if (currentOverlapCount > 0)
        {
            TryApplyBoost();
        }
    }

    protected override void OnAbilityDisable()
    {
        // 当面失活时（比如翻滚到其他面了），强制移除增强
        RemoveBoost();
    }

    // --- 2. 物理碰撞：只负责计数 ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 校验：图层匹配 + (面激活 或 不要求面激活)
        if (!IsInLayerMask(other.gameObject.layer)) return;
        if (requireCorrectFace && !isFaceActive) return;

        currentOverlapCount++;
        TryApplyBoost();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject.layer)) return;

        currentOverlapCount = Mathf.Max(0, currentOverlapCount - 1);
        if (currentOverlapCount == 0)
        {
            RemoveBoost();
        }
    }

    // --- 3. 核心逻辑：应用/移除增强 ---
    private void TryApplyBoost()
    {
        // 双重条件：面是激活的 + 还没应用过增强
        if (!isFaceActive || isBoostApplied || blockController == null) return;

        // 1. 记录原始数值
        originalGroundJumpForce = blockController.groundJumpForce;
        originalWallJumpForce = blockController.wallJumpForce;

        // 2. 应用倍率
        blockController.groundJumpForce = originalGroundJumpForce * jumpBoostMultiplier;
        blockController.wallJumpForce = originalWallJumpForce * jumpBoostMultiplier;

        isBoostApplied = true;
        Debug.Log($"【弹跳面】激活！跳跃力变为 {jumpBoostMultiplier} 倍", this);
    }

    private void RemoveBoost()
    {
        // 只有应用过增强，且controller还在，才还原
        if (!isBoostApplied || blockController == null) return;

        // 安全还原回原始数值
        blockController.groundJumpForce = originalGroundJumpForce;
        blockController.wallJumpForce = originalWallJumpForce;

        isBoostApplied = false;
        Debug.Log($"【弹跳面】移除，跳跃力已还原", this);
    }

    // --- 4. 辅助工具：判断图层 ---
    private bool IsInLayerMask(int layer)
    {
        return (surfaceLayers.value & (1 << layer)) != 0;
    }

    // --- 5. 安全兜底：脚本禁用/销毁时强制还原 ---
    private void OnDisable()
    {
        RemoveBoost();
    }

    private void OnDestroy()
    {
        RemoveBoost();
    }
}