using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class AdvancedBlockController : MonoBehaviour
{
    #region 状态机
    public enum PlayerState { Grounded, Airborne, WallClinging, Rolling, Dashing }
    public PlayerState CurrentState { get; private set; } = PlayerState.Airborne;
    #endregion

    #region 移动参数
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 80f;
    [SerializeField] private float deceleration = 70f;
    [SerializeField] private float airControlMultiplier = 0.6f;
    #endregion

    #region 跳跃参数
    [Header("跳跃参数")]
    [SerializeField] private float groundJumpForce = 12f;
    [SerializeField] private float wallJumpForce = 14f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private bool isJumping;
    #endregion

    #region 翻滚与冲刺
    [Header("翻滚与冲刺")]
    [SerializeField] private float rollSpeed = 15f;
    [SerializeField] private float rollDuration = 0.3f;
    [SerializeField] private float rollCooldown = 0.5f;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.8f;
    private float lastRollTime = -10f;
    private float lastDashTime = -10f;
    private Vector2 rollDirection;
    private Vector2 dashDirection;
    #endregion

    #region 物理组件
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private float originalGravity;
    #endregion

    #region 检测系统
    [Header("检测参数")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float wallCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    public enum GroundSide { None, Bottom, Top, Left, Right }
    private GroundSide currentGroundSide = GroundSide.None;

    // 缓存变换引用
    private Transform cachedTransform;

    // 检测点计算（使用缓存）
    private Vector2 BottomCheckPos => (Vector2)cachedTransform.position - (Vector2)cachedTransform.up * boxCollider.bounds.extents.y;
    private Vector2 TopCheckPos => (Vector2)cachedTransform.position + (Vector2)cachedTransform.up * boxCollider.bounds.extents.y;
    private Vector2 LeftCheckPos => (Vector2)cachedTransform.position - (Vector2)cachedTransform.right * boxCollider.bounds.extents.x;
    private Vector2 RightCheckPos => (Vector2)cachedTransform.position + (Vector2)cachedTransform.right * boxCollider.bounds.extents.x;
    #endregion

    #region 输入系统
    private Vector2 moveInput;
    private bool jumpInput;
    private bool jumpInputReleased;
    private bool rollInput;
    private bool dashInput;
    #endregion

    #region 可视化调试
    [Header("调试")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color groundCheckColor = Color.green;
    [SerializeField] private Color wallCheckColor = Color.blue;
    #endregion

    #region 初始化
    void Awake()
    {
        // 缓存变换引用
        cachedTransform = transform;

        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        originalGravity = rb.gravityScale;
    }
    #endregion

    #region 主循环
    void Update()
    {
        GatherInput();
        UpdateTimers();
        HandleJumpInput();
        HandleStateTransitions();
    }

    void FixedUpdate()
    {
        UpdateGroundDetection();
        HandlePhysicsBasedMovement();
        ApplyGravityModifiers();
    }
    #endregion

    #region 输入处理
    private void GatherInput()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpInput = true;
            lastJumpPressedTime = Time.time;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpInputReleased = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            rollInput = true;
        }

        if (Input.GetMouseButtonDown(1))
        {
            dashInput = true;
        }
    }
    #endregion

    #region 物理移动
    private void HandlePhysicsBasedMovement()
    {
        switch (CurrentState)
        {
            case PlayerState.Grounded:
                HandleGroundMovement();
                break;

            case PlayerState.Airborne:
                HandleAirMovement();
                break;

            case PlayerState.WallClinging:
                HandleWallMovement();
                break;

            case PlayerState.Rolling:
                HandleRollMovement();
                break;

            case PlayerState.Dashing:
                HandleDashMovement();
                break;
        }
    }

    private void HandleGroundMovement()
    {
        // 计算目标速度（基于当前朝向）
        Vector2 targetVelocity = new Vector2(
            moveInput.x * moveSpeed,
            moveInput.y * moveSpeed
        );

        // 应用平滑加速/减速
        Vector2 localVelocity = new Vector2(
            Vector2.Dot(rb.velocity, cachedTransform.right),
            Vector2.Dot(rb.velocity, cachedTransform.up)
        );

        float newXVel = Mathf.MoveTowards(
            localVelocity.x,
            targetVelocity.x,
            (Mathf.Abs(targetVelocity.x) > 0.01f ? acceleration : deceleration) * Time.fixedDeltaTime
        );

        float newYVel = Mathf.MoveTowards(
            localVelocity.y,
            targetVelocity.y,
            (Mathf.Abs(targetVelocity.y) > 0.01f ? acceleration : deceleration) * Time.fixedDeltaTime
        );

        // 更新速度
        rb.velocity = (Vector2)cachedTransform.right * newXVel + (Vector2)cachedTransform.up * newYVel;
    }

    private void HandleAirMovement()
    {
        // 在空中时减少控制能力
        float controlMultiplier = airControlMultiplier;

        // 计算目标速度
        Vector2 targetVelocity = new Vector2(
            moveInput.x * moveSpeed * controlMultiplier,
            moveInput.y * moveSpeed * controlMultiplier
        );

        // 获取当前本地速度
        Vector2 localVelocity = new Vector2(
            Vector2.Dot(rb.velocity, cachedTransform.right),
            Vector2.Dot(rb.velocity, cachedTransform.up)
        );

        // 应用平滑加速度
        float newXVel = Mathf.MoveTowards(
            localVelocity.x,
            targetVelocity.x,
            acceleration * controlMultiplier * Time.fixedDeltaTime
        );

        float newYVel = Mathf.MoveTowards(
            localVelocity.y,
            targetVelocity.y,
            acceleration * controlMultiplier * Time.fixedDeltaTime
        );

        // 计算全局速度
        Vector2 globalVelocity =
            (Vector2)cachedTransform.right * newXVel +
            (Vector2)cachedTransform.up * newYVel;

        // 保持现有垂直速度
        Vector2 globalDown = Physics2D.gravity.normalized;
        float verticalVelocity = Vector2.Dot(rb.velocity, globalDown);

        // 只在没有输入时保持下落速度
        if (Mathf.Abs(moveInput.y) < 0.1f && verticalVelocity < 0)
        {
            globalVelocity += globalDown * verticalVelocity;
        }

        rb.velocity = globalVelocity;
    }

    private void HandleWallMovement()
    {
        // 墙面吸附时只允许上下移动
        float targetYVel = moveInput.y * moveSpeed;

        // 获取当前本地速度
        float currentYVel = Vector2.Dot(rb.velocity, cachedTransform.up);
        float newYVel = Mathf.MoveTowards(
            currentYVel,
            targetYVel,
            acceleration * Time.fixedDeltaTime
        );

        // 锁定水平移动
        float currentXVel = Vector2.Dot(rb.velocity, cachedTransform.right);
        float newXVel = Mathf.MoveTowards(currentXVel, 0f, deceleration * 2 * Time.fixedDeltaTime);

        rb.velocity = (Vector2)cachedTransform.right * newXVel + (Vector2)cachedTransform.up * newYVel;
    }
    #endregion

    #region 跳跃系统
    private void HandleJumpInput()
    {
        // 跳跃键释放时减少跳跃高度
        if (jumpInputReleased)
        {
            if (rb.velocity.y > 0 && isJumping)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
            }
            jumpInputReleased = false;
        }

        // 检查跳跃缓冲
        if (Time.time - lastJumpPressedTime <= jumpBufferTime)
        {
            TryJump();
        }

        // 重置跳跃输入
        if (jumpInput)
        {
            jumpInput = false;
        }
    }

    private void TryJump()
    {
        // 土狼时间跳跃
        if (Time.time - lastGroundedTime <= coyoteTime && !isJumping)
        {
            ExecuteJump(GetJumpDirection(currentGroundSide), groundJumpForce);
            return;
        }

        // 墙面跳跃
        if (CurrentState == PlayerState.WallClinging)
        {
            ExecuteJump(GetWallJumpDirection(currentGroundSide), wallJumpForce);
            SetState(PlayerState.Airborne);
            return;
        }
    }

    private Vector2 GetJumpDirection(GroundSide side)
    {
        switch (side)
        {
            case GroundSide.Bottom: return cachedTransform.up;
            case GroundSide.Top: return -cachedTransform.up;
            case GroundSide.Left: return cachedTransform.right;
            case GroundSide.Right: return -cachedTransform.right;
            default: return Vector2.up;
        }
    }

    private Vector2 GetWallJumpDirection(GroundSide side)
    {
        Vector2 awayFromWall = side switch
        {
            GroundSide.Left => (Vector2)cachedTransform.right,
            GroundSide.Right => -(Vector2)cachedTransform.right,
            _ => Vector2.zero
        };

        // 组合向上和远离墙面的方向
        Vector2 combinedDirection = awayFromWall + (Vector2)cachedTransform.up * 0.7f;

        // 确保方向有效
        if (combinedDirection.magnitude < 0.1f)
        {
            combinedDirection = Vector2.up;
        }

        return combinedDirection.normalized;
    }

    private void ExecuteJump(Vector2 direction, float force)
    {
        // 重置垂直速度
        Vector2 currentVelocity = rb.velocity;
        currentVelocity.y = 0;
        rb.velocity = currentVelocity;

        // 应用跳跃力
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        isJumping = true;
        lastJumpPressedTime = -10f; // 消耗缓冲
    }
    #endregion

    #region 特殊动作
    private void HandleRollMovement()
    {
        rb.velocity = rollDirection * rollSpeed;
    }

    private void HandleDashMovement()
    {
        rb.velocity = dashDirection * dashSpeed;
    }

    private IEnumerator RollRoutine(Vector2 direction)
    {
        SetState(PlayerState.Rolling);
        rollDirection = direction;

        // 翻滚时缩小碰撞体避免卡墙
        Vector2 originalSize = boxCollider.size;
        boxCollider.size = originalSize * 0.8f;

        yield return new WaitForSeconds(rollDuration);

        if (CurrentState == PlayerState.Rolling)
        {
            SetState(IsGrounded() ? PlayerState.Grounded : PlayerState.Airborne);
        }

        boxCollider.size = originalSize;
        lastRollTime = Time.time;
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        SetState(PlayerState.Dashing);
        dashDirection = direction;

        // 冲刺时短暂无敌
        int originalLayer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(originalLayer, LayerMask.NameToLayer("Enemy"), true);

        yield return new WaitForSeconds(dashDuration);

        if (CurrentState == PlayerState.Dashing)
        {
            SetState(IsGrounded() ? PlayerState.Grounded : PlayerState.Airborne);
        }

        Physics2D.IgnoreLayerCollision(originalLayer, LayerMask.NameToLayer("Enemy"), false);
        lastDashTime = Time.time;
    }
    #endregion

    #region 状态管理
    private void SetState(PlayerState newState)
    {
        if (CurrentState == newState) return;

        // 退出状态
        switch (CurrentState)
        {
            case PlayerState.WallClinging:
                rb.gravityScale = originalGravity;
                break;
        }

        // 进入状态
        switch (newState)
        {
            case PlayerState.Grounded:
                isJumping = false;
                break;

            case PlayerState.WallClinging:
                rb.gravityScale = 0f;
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Min(rb.velocity.y, 0)); // 防止向上滑动
                break;
        }

        CurrentState = newState;
    }

    private void HandleStateTransitions()
    {
        // 检查翻滚输入
        if (rollInput && Time.time >= lastRollTime + rollCooldown)
        {
            // 修复的鼠标位置计算
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = -Camera.main.transform.position.z; // 设置合适的深度
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            // 正确的方向计算
            Vector2 rollDir = (new Vector2(mouseWorldPos.x, mouseWorldPos.y) - (Vector2)cachedTransform.position).normalized;

            // 确保方向有效
            if (rollDir.magnitude > 0.1f)
            {
                StartCoroutine(RollRoutine(rollDir));
            }

            rollInput = false;
            return;
        }

        // 检查冲刺输入
        if (dashInput && Time.time >= lastDashTime + dashCooldown &&
            (CurrentState == PlayerState.Grounded || CurrentState == PlayerState.Airborne))
        {
            Vector2 dashDir = moveInput.magnitude > 0.1f ? moveInput.normalized : (Vector2)cachedTransform.up;
            StartCoroutine(DashRoutine(dashDir));
            dashInput = false;
            return;
        }

        // 自动状态转换
        if (CurrentState == PlayerState.Rolling || CurrentState == PlayerState.Dashing) return;

        if (IsGrounded())
        {
            SetState(PlayerState.Grounded);
        }
        else if (IsTouchingWall() && rb.velocity.y <= 0)
        {
            SetState(PlayerState.WallClinging);
        }
        else
        {
            SetState(PlayerState.Airborne);
        }
    }
    #endregion

    #region 物理辅助
    private void ApplyGravityModifiers()
    {
        // 添加自定义重力效果
        if (CurrentState == PlayerState.Airborne && rb.velocity.y < 0)
        {
            rb.gravityScale = originalGravity * 1.5f; // 下落重力增强
        }
        else
        {
            rb.gravityScale = originalGravity;
        }
    }

    private void UpdateTimers()
    {
        if (IsGrounded())
        {
            lastGroundedTime = Time.time;
        }
    }
    #endregion

    #region 检测系统
    private void UpdateGroundDetection()
    {
        currentGroundSide = GroundSide.None;

        // 使用Raycast检测更精确
        if (CheckGround(-cachedTransform.up, BottomCheckPos))
            currentGroundSide = GroundSide.Bottom;
        else if (CheckGround(cachedTransform.up, TopCheckPos))
            currentGroundSide = GroundSide.Top;
        else if (CheckGround(-cachedTransform.right, LeftCheckPos))
            currentGroundSide = GroundSide.Left;
        else if (CheckGround(cachedTransform.right, RightCheckPos))
            currentGroundSide = GroundSide.Right;
    }

    private bool CheckGround(Vector2 direction, Vector2 checkPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            checkPos,
            direction,
            groundCheckDistance,
            groundLayer
        );
        return hit.collider != null;
    }

    private bool IsGrounded()
    {
        return currentGroundSide != GroundSide.None;
    }

    private bool IsTouchingWall()
    {
        return currentGroundSide == GroundSide.Left || currentGroundSide == GroundSide.Right;
    }
    #endregion

    #region 调试可视化
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        // 确保在编辑器中有引用
        if (cachedTransform == null) cachedTransform = transform;
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        // 绘制地面检测
        Gizmos.color = groundCheckColor;
        Gizmos.DrawLine(BottomCheckPos, BottomCheckPos - (Vector2)cachedTransform.up * groundCheckDistance);
        Gizmos.DrawLine(TopCheckPos, TopCheckPos + (Vector2)cachedTransform.up * groundCheckDistance);
        Gizmos.DrawLine(LeftCheckPos, LeftCheckPos - (Vector2)cachedTransform.right * groundCheckDistance);
        Gizmos.DrawLine(RightCheckPos, RightCheckPos + (Vector2)cachedTransform.right * groundCheckDistance);

        // 绘制当前状态
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 14;
        UnityEditor.Handles.Label(cachedTransform.position + Vector3.up * 0.7f,
                                 $"State: {CurrentState}\nSide: {currentGroundSide}", style);
    }
    #endregion
}