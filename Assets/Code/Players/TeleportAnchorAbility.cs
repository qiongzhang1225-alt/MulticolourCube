using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class TeleportAnchorAbility : FaceAbilityBase
{
    public enum AbilityState { Idle, HasAnchor }

    [Header("接触判定")]
    [Tooltip("仅用于视觉参考，实际触发以BlockController的面激活为准")]
    public LayerMask surfaceLayers;

    [Header("虚影设置")]
    public bool showGhostPreview = true;
    [Range(0.1f, 0.6f)] public float ghostAlpha = 0.4f;
    public Color ghostColor = new Color(0.2f, 0.6f, 1f, 0.4f);

    [Header("传送设置")]
    public bool resetVelocityOnTeleport = true;
    [Tooltip("传送后短暂禁用碰撞体（防止卡墙/起飞）")]
    public float disableCollisionTime = 0.15f;

    // 内部状态
    private AbilityState currentState = AbilityState.Idle;
    private Vector3 anchorWorldPosition;
    private Quaternion anchorRotation;
    private GameObject ghostObject;
    private Collider2D col;
    private Rigidbody2D rb;
    private Collider2D parentCollider;

    protected override void Awake()
    {
        base.Awake();

        // 获取组件（碰撞体仅用于满足RequireComponent，实际不做检测）
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // 获取父物体核心组件
        rb = GetComponentInParent<Rigidbody2D>();
        parentCollider = GetComponentInParent<BoxCollider2D>();
        blockController = GetComponentInParent<BlockController>();

        if (rb == null || parentCollider == null)
        {
            Debug.LogError($"【传送锚点错误】找不到 Rigidbody2D 或 BoxCollider2D！", this);
            enabled = false;
        }
    }

    protected override void OnAbilityEnable()
    {
        // 面激活时，如果有虚影则显示
        if (ghostObject != null)
            ghostObject.SetActive(true);
    }

    protected override void OnAbilityDisable()
    {
        // 面失活时，隐藏虚影（保留数据）
        if (ghostObject != null)
            ghostObject.SetActive(false);
    }

    // ==================== 核心简化：移除了所有OnTrigger检测 ====================
    // 完全信任 BlockController 的 isFaceActive 判断

    public override void AbilityUpdate()
    {
        // 【核心逻辑】只要面激活 + 按鼠标右键，就触发！
        // 因为 BlockController 的 isFaceActive = true 本身就意味着“这个面接地了”
        if (!isFaceActive || !Input.GetMouseButtonDown(1))
            return;

        switch (currentState)
        {
            case AbilityState.Idle:
                SetAnchor();
                break;
            case AbilityState.HasAnchor:
                StartCoroutine(PerformTeleport());
                break;
        }
    }

    // ==================== 以下保持不变：稳定的锚点与传送逻辑 ====================

    private void SetAnchor()
    {
        // 直接记录父物体（角色本体）的世界坐标和旋转
        Transform parent = transform.parent;
        anchorWorldPosition = parent.position;
        anchorRotation = parent.rotation;

        currentState = AbilityState.HasAnchor;

        if (showGhostPreview)
        {
            CreateGhost();
        }

        Debug.Log($"【锚点已设置】世界坐标: {anchorWorldPosition}");
    }

    private System.Collections.IEnumerator PerformTeleport()
    {
        Debug.Log($"【开始传送】目标: {anchorWorldPosition}");
        Transform parent = transform.parent;

        // 1. 完全冻结刚体，防止物理干扰
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.isKinematic = true;

        // 2. 短暂禁用主碰撞体
        if (parentCollider != null)
            parentCollider.enabled = false;

        // 3. 等待一帧
        yield return null;

        // 4. 执行传送
        parent.SetPositionAndRotation(anchorWorldPosition, anchorRotation);

        // 5. 再等待一帧
        yield return null;

        // 6. 恢复物理系统
        rb.isKinematic = false;
        if (parentCollider != null)
            parentCollider.enabled = true;

        // 7. 双重保险清零速度
        if (resetVelocityOnTeleport)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 8. 清理锚点
        ClearAnchor();

        Debug.Log($"【传送完成】当前位置: {parent.position}");
    }

    private void CreateGhost()
    {
        if (ghostObject != null)
            Destroy(ghostObject);

        SpriteRenderer parentSprite = GetComponentInParent<SpriteRenderer>();

        if (parentSprite != null)
        {
            ghostObject = new GameObject("TeleportAnchor_Ghost");
            ghostObject.transform.SetPositionAndRotation(anchorWorldPosition, anchorRotation);

            SpriteRenderer ghostSr = ghostObject.AddComponent<SpriteRenderer>();
            ghostSr.sprite = parentSprite.sprite;
            ghostSr.color = ghostColor;
            ghostSr.sortingOrder = parentSprite.sortingOrder - 1;
            ghostSr.flipX = parentSprite.flipX;
            ghostSr.flipY = parentSprite.flipY;
        }
        else
        {
            Debug.LogWarning("【传送锚点】找不到 SpriteRenderer，无法生成虚影。");
        }
    }

    private void ClearAnchor()
    {
        currentState = AbilityState.Idle;
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
        }
    }

    private void OnDisable()
    {
        if (ghostObject != null)
            ghostObject.SetActive(false);
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (ghostObject != null)
            Destroy(ghostObject);
        StopAllCoroutines();
    }
}