using UnityEngine;

public class SurfaceAwareController : MonoBehaviour
{
    // 地面检测面枚举
    public enum SurfaceSide { None, Bottom, Top, Left, Right }

    [Header("移动参数")]
    public float moveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 50f;
    public float airControl = 0.3f;

    [Header("跳跃参数")]
    public float jumpForce = 12f;

    [Header("地面检测")]
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public Transform bottomCheck;
    public Transform topCheck;
    public Transform leftCheck;
    public Transform rightCheck;

    [Header("旋转（预留）")]
    public float rotationSpeed = 180f;   // 度/秒
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    // 当前着地面
    public SurfaceSide currentGroundSide { get; private set; } = SurfaceSide.None;
    // 是否任意面着地
    public bool isGrounded => currentGroundSide != SurfaceSide.None;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool jumpPressed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 如果没有指定检测点，尝试自动创建（仅作示例，建议手动设置）
        if (bottomCheck == null) bottomCheck = CreateCheckPoint("BottomCheck", new Vector3(0, -0.5f, 0));
        if (topCheck == null) topCheck = CreateCheckPoint("TopCheck", new Vector3(0, 0.5f, 0));
        if (leftCheck == null) leftCheck = CreateCheckPoint("LeftCheck", new Vector3(-0.5f, 0, 0));
        if (rightCheck == null) rightCheck = CreateCheckPoint("RightCheck", new Vector3(0.5f, 0, 0));
    }

    void Update()
    {
        // 水平输入
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 跳跃输入
        if (Input.GetKeyDown(KeyCode.Space))
            jumpPressed = true;

        // 旋转输入（后续可扩展）
        if (Input.GetKey(rotateLeftKey))
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        if (Input.GetKey(rotateRightKey))
            transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        // 更新地面检测
        UpdateGrounded();

        // 水平移动（带加速度）
        float targetSpeed = horizontalInput * moveSpeed;
        float accel = isGrounded ? acceleration : acceleration * airControl;

        if (horizontalInput != 0)
        {
            rb.velocity = new Vector2(
                Mathf.MoveTowards(rb.velocity.x, targetSpeed, accel * Time.fixedDeltaTime),
                rb.velocity.y
            );
        }
        else
        {
            rb.velocity = new Vector2(
                Mathf.MoveTowards(rb.velocity.x, 0, deceleration * Time.fixedDeltaTime),
                rb.velocity.y
            );
        }

        // 跳跃处理
        if (jumpPressed && isGrounded)
        {
            // 根据着地面确定跳跃方向
            Vector2 jumpDirection = GetJumpDirection();
            rb.velocity = new Vector2(rb.velocity.x, 0) + jumpDirection * jumpForce;
            jumpPressed = false;
        }
        else
        {
            jumpPressed = false;
        }
    }

    // 更新四个面的检测状态
    void UpdateGrounded()
    {
        bool bottom = Physics2D.OverlapCircle(bottomCheck.position, groundCheckRadius, groundLayer);
        bool top = Physics2D.OverlapCircle(topCheck.position, groundCheckRadius, groundLayer);
        bool left = Physics2D.OverlapCircle(leftCheck.position, groundCheckRadius, groundLayer);
        bool right = Physics2D.OverlapCircle(rightCheck.position, groundCheckRadius, groundLayer);

        // 优先级：底部 > 左侧 > 右侧 > 顶部（可根据需要调整）
        if (bottom) currentGroundSide = SurfaceSide.Bottom;
        else if (left) currentGroundSide = SurfaceSide.Left;
        else if (right) currentGroundSide = SurfaceSide.Right;
        else if (top) currentGroundSide = SurfaceSide.Top;
        else currentGroundSide = SurfaceSide.None;
    }

    // 根据着地面返回跳跃方向（世界向量）
    Vector2 GetJumpDirection()
    {
        switch (currentGroundSide)
        {
            case SurfaceSide.Bottom: return transform.up;
            case SurfaceSide.Top: return -transform.up;
            case SurfaceSide.Left: return transform.right;
            case SurfaceSide.Right: return -transform.right;
            default: return Vector2.zero;
        }
    }

    // 辅助方法：创建检测点子物体（仅用于快速测试）
    Transform CreateCheckPoint(string name, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = transform;
        go.transform.localPosition = localPos;
        return go.transform;
    }

    // 可视化地面检测范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (bottomCheck) Gizmos.DrawWireSphere(bottomCheck.position, groundCheckRadius);
        if (topCheck) Gizmos.DrawWireSphere(topCheck.position, groundCheckRadius);
        if (leftCheck) Gizmos.DrawWireSphere(leftCheck.position, groundCheckRadius);
        if (rightCheck) Gizmos.DrawWireSphere(rightCheck.position, groundCheckRadius);
    }
}