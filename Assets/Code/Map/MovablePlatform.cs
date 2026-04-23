using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 可移动平台：运动学刚体驱动。
/// 玩家通过位移增量跟随（不使用 SetParent，避免旋转时 scale 变形）。
/// 实现 IResettable，死亡/检查点时自动恢复状态。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MovablePlatform : MonoBehaviour, IResettable
{
    [Header("移动参数")]
    public Vector2 moveDirection = Vector2.right;
    [Min(0.1f)] public float moveSpeed = 2f;
    [Min(0.5f)] public float moveDistance = 3f;
    public bool isLoop = true;

    private Rigidbody2D rb;
    private Vector2 startPos;
    private float movedDistance;
    private int moveDirSign = 1;

    // ── 跟随系统（替代 SetParent）──
    private readonly List<Transform> riders = new List<Transform>();

    // ── 检查点存档 ──
    private Vector3 savedPosition;
    private Vector2 savedStartPos;
    private float savedMovedDistance;
    private int savedMoveDirSign;
    private bool savedEnabled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        startPos = transform.position;
    }

    private void Update()
    {
        Vector3 prevPos = transform.position;
        MovePlatform();

        Vector3 delta = transform.position - prevPos;
        if (delta.sqrMagnitude > 0.00001f)
        {
            for (int i = riders.Count - 1; i >= 0; i--)
            {
                if (riders[i] != null)
                    riders[i].position += delta;
                else
                    riders.RemoveAt(i);
            }
        }
    }

    public void MovePlatform()
    {
        if (moveDirection.magnitude < 0.1f) return;

        Vector2 moveStep = moveDirection.normalized * moveSpeed * Time.deltaTime * moveDirSign;
        transform.Translate(moveStep);
        movedDistance = Vector2.Distance(startPos, transform.position);

        if (movedDistance >= moveDistance)
        {
            if (isLoop)
            {
                moveDirSign *= -1;
                startPos = transform.position;
                movedDistance = 0;
            }
            else
            {
                enabled = false;
            }
        }
    }

    // ── 碰撞：跟随注册 ──

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Transform t = collision.collider.transform;
            if (!riders.Contains(t))
                riders.Add(t);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
            riders.Remove(collision.collider.transform);
    }

    // ── IResettable ──

    public void SaveCheckpointState()
    {
        savedPosition = transform.position;
        savedStartPos = startPos;
        savedMovedDistance = movedDistance;
        savedMoveDirSign = moveDirSign;
        savedEnabled = enabled;
    }

    public void ResetToCheckpointState()
    {
        transform.position = savedPosition;
        startPos = savedStartPos;
        movedDistance = savedMovedDistance;
        moveDirSign = savedMoveDirSign;
        enabled = savedEnabled;
        riders.Clear();
    }
}
