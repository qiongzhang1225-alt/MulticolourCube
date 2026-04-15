using UnityEngine;

[RequireComponent(typeof(Collider2D))] // 确保有碰撞体（设为Trigger/非Trigger均可）
[RequireComponent(typeof(Rigidbody2D))]
public class MovablePlatform : MonoBehaviour
{
    [Header("移动设置")]
    public Vector2 moveDirection = Vector2.right; // 移动方向（右/上/左/下）
    [Min(0.1f)] public float moveSpeed = 2f;      // 移动速度
    [Min(0.5f)] public float moveDistance = 3f;   // 单次移动最大距离
    public bool isLoop = true;                    // 是否往复运动

    private Rigidbody2D rb;
    private Vector2 startPos;       // 初始位置
    private float movedDistance;    // 已移动距离
    private int moveDirSign = 1;    // 移动方向符号（1/-1，控制往复）

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // 运动学刚体（不受外力，手动控制）
        rb.gravityScale = 0;                     // 无重力
        startPos = transform.position;
    }

    private void Update()
    {
        MovePlatform();
    }

    /// <summary>
    /// 核心移动逻辑
    /// </summary>
    public void MovePlatform()
    {
        if (moveDirection.magnitude < 0.1f) return; // 避免无效方向

        // 计算本次移动增量
        Vector2 moveStep = moveDirection.normalized * moveSpeed * Time.deltaTime * moveDirSign;
        transform.Translate(moveStep);

        // 计算已移动距离（相对初始位置）
        movedDistance = Vector2.Distance(startPos, transform.position);

        // 到达最大距离，切换方向（往复）或停止
        if (movedDistance >= moveDistance)
        {
            if (isLoop)
            {
                moveDirSign *= -1; // 反转方向
                startPos = transform.position; // 重置初始位置（避免累计误差）
                movedDistance = 0;
            }
            else
            {
                enabled = false; // 非循环则停止移动
            }
        }
    }

    // 可选：玩家站在地块上时跟随移动（需将地块碰撞体设为非Trigger）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.transform.SetParent(transform); // 玩家设为子物体，跟随移动
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.transform.SetParent(null); // 离开后解除父子关系
        }
    }
}