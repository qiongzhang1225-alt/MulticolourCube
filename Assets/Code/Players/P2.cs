using UnityEngine;
using System;

public class BlockController : MonoBehaviour
{
    public enum GroundSide { None, Bottom, Top, Left, Right }

    [Header("移动参数")]
    public float moveSpeed = 5f;
    float wallSlideSpeed = 3f;

    [Header("跳跃参数")]
    public float groundJumpForce = 7f;
    public float wallJumpForce = 9f;

    [Header("翻滚参数")]
    public float rollDuration = 0.2f;
    public float rollCooldown = 0.5f;

    [Header("地面检测")]
    public Transform groundCheckBottom;
    public Transform groundCheckTop;
    public Transform groundCheckLeft;
    public Transform groundCheckRight;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("四面能力组件")]
    public GameObject bottomFace;
    public GameObject topFace;
    public GameObject leftFace;
    public GameObject rightFace;

    // ========== 核心变量 ==========
    private Rigidbody2D rb;
    private BoxCollider2D box;
    private bool isRolling = false;
    private float lastRollTime = -999f;

    // ========== 修改后的核心变量 ==========
    // 1. 私有字段（加下划线，仅内部可修改）
    private bool _isGrounded = false;

    // 2. 公共只读属性（外部只能读取，不能修改）
    public bool isGrounded => _isGrounded;

    // 优化：用下划线区分私有字段，提供公共只读属性
    [SerializeField] private GroundSide _currentGroundSide = GroundSide.None;
    public GroundSide currentGroundSide => _currentGroundSide;

    private bool isBottomGrounded = false;
    private bool isTopGrounded = false;
    public bool isLeftGrounded = false;  // 改为public，方便优化3复用
    public bool isRightGrounded = false; // 改为public，方便优化3复用
    private float originalGravity;

    private bool isWallClinging = false;
    public bool IsWallClinging => isWallClinging;

    // ========== 优化1：新增缓存输入的变量 ==========
    private float inputX;
    private float inputY;

    // ========== 事件系统 ==========
    public event Action<GroundSide, GameObject> OnGroundSideChanged;
    private GroundSide lastGroundSide = GroundSide.None;
    private GameObject lastActiveFace = null;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        originalGravity = rb.gravityScale;
    }

    void Update()
    {
        UpdateGrounded();

        // ========== 优化1：Update 只负责收集输入和瞬间响应 ==========
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");

        if (!isRolling)
        {
            HandleJump();
            HandleRollInput();
        }

        UpdateActiveFaceAbility();
    }

    // ========== 优化1：新增 FixedUpdate，只负责物理移动 ==========
    void FixedUpdate()
    {
        if (!isRolling)
        {
            HandleMovement(inputX, inputY);
        }
    }

    void UpdateGrounded()
    {
        // ✅ 修复：使用方块【自身局部坐标系】检测六个面（正确！）
        Vector2 bottomDir = -transform.up;    // 自身底部
        Vector2 topDir = transform.up;       // 自身顶部
        Vector2 leftDir = -transform.right;  // 自身左侧
        Vector2 rightDir = transform.right;  // 自身右侧

        float extentY = box.bounds.extents.y;
        float extentX = box.bounds.extents.x;

        // ✅ 正确检测：每个面朝着【自己的方向】发射检测
        isBottomGrounded = Physics2D.OverlapCircle((Vector2)transform.position + bottomDir * extentY, groundCheckRadius, groundLayer);
        isTopGrounded = Physics2D.OverlapCircle((Vector2)transform.position + topDir * extentY, groundCheckRadius, groundLayer);
        isLeftGrounded = Physics2D.OverlapCircle((Vector2)transform.position + leftDir * extentX, groundCheckRadius, groundLayer);
        isRightGrounded = Physics2D.OverlapCircle((Vector2)transform.position + rightDir * extentX, groundCheckRadius, groundLayer);

        _isGrounded = isBottomGrounded || isTopGrounded || isLeftGrounded || isRightGrounded;

        // 记录上一帧状态
        lastGroundSide = _currentGroundSide;
        lastActiveFace = GetFaceBySide(lastGroundSide);

        // 更新当前接触的面
        _currentGroundSide = GroundSide.None;
        if (isBottomGrounded) _currentGroundSide = GroundSide.Bottom;
        else if (isTopGrounded) _currentGroundSide = GroundSide.Top;
        else if (isLeftGrounded) _currentGroundSide = GroundSide.Left;
        else if (isRightGrounded) _currentGroundSide = GroundSide.Right;

        // 状态变更事件
        GameObject currentActiveFace = GetFaceBySide(_currentGroundSide);
        if (_currentGroundSide != lastGroundSide)
        {
            // 上一个面失活
            if (lastActiveFace != null)
            {
                FaceAbilityBase[] lastAbilities = lastActiveFace.GetComponents<FaceAbilityBase>();
                foreach (var ability in lastAbilities)
                {
                    ability.OnFaceDeactivated();
                }
            }

            // 新的面激活
            if (currentActiveFace != null)
            {
                FaceAbilityBase[] currentAbilities = currentActiveFace.GetComponents<FaceAbilityBase>();
                foreach (var ability in currentAbilities)
                {
                    ability.OnFaceActivated();
                }
            }

            OnGroundSideChanged?.Invoke(_currentGroundSide, currentActiveFace);
        }
    }

    // ========== 优化3 + 清理冰面：全新的 HandleMovement ==========
    void HandleMovement(float horizontalInput, float verticalInput)
    {
        // 优化3：直接复用 UpdateGrounded 的 isLeftGrounded/isRightGrounded
        bool wallClingingActive = isWallClinging && (isLeftGrounded || isRightGrounded);

        if (wallClingingActive)
        {
            // 模式 A：墙面吸附
            float slideVelocity = verticalInput * wallSlideSpeed;
            rb.velocity = new Vector2(0, slideVelocity);
        }
        else
        {
            // 模式 B：普通移动（已彻底删除冰面逻辑）
            float targetVelocity = horizontalInput * moveSpeed;

            // 地面：瞬间响应；空中：轻微惯性
            float airSmoothFactor = _isGrounded ? 1.0f : 0.3f;
            float newX = Mathf.Lerp(rb.velocity.x, targetVelocity, airSmoothFactor);
            rb.velocity = new Vector2(newX, rb.velocity.y);
        }
    }

    // ========== 已删除：IsTouchingWall 方法（优化3不再需要） ==========

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2 jumpDir = Vector2.zero;
            float jumpPower = 0f;
            Vector2 currentVelocity = rb.velocity;

            if (isWallClinging)
            {
                switch (_currentGroundSide)
                {
                    case GroundSide.Left:
                        jumpDir = transform.right;
                        break;
                    case GroundSide.Right:
                        jumpDir = -transform.right;
                        break;
                }
                jumpPower = wallJumpForce;
                SetWallCling(false);

                rb.velocity = new Vector2(currentVelocity.x * 0.3f, 0) + jumpDir * jumpPower;
            }
            else if (isGrounded)
            {
                switch (_currentGroundSide)
                {
                    case GroundSide.Bottom:
                        jumpDir = transform.up;
                        break;
                    case GroundSide.Top:
                        jumpDir = -transform.up;
                        break;
                    case GroundSide.Left:
                        jumpDir = transform.right;
                        break;
                    case GroundSide.Right:
                        jumpDir = -transform.right;
                        break;
                }
                jumpPower = groundJumpForce;

                rb.velocity = new Vector2(currentVelocity.x, currentVelocity.y) + jumpDir * jumpPower;
            }
        }
    }

    void HandleRollInput()
    {
        if (Time.time - lastRollTime < rollCooldown) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int direction = mouseWorldPos.x > transform.position.x ? 1 : -1;
            StartCoroutine(Roll(direction));
        }
    }

    System.Collections.IEnumerator Roll(int direction)
    {
        isRolling = true;
        lastRollTime = Time.time;

        Bounds bounds = box.bounds;
        Vector3 pivotPoint = transform.position;
        float offset = bounds.extents.x;

        switch (_currentGroundSide)
        {
            case GroundSide.Bottom:
                pivotPoint = transform.position + (transform.right * direction * offset) - transform.up * offset;
                break;
            case GroundSide.Top:
                pivotPoint = transform.position - (transform.right * direction * offset) + transform.up * offset;
                break;
            case GroundSide.Left:
                pivotPoint = transform.position - transform.right * offset - transform.up * direction * offset;
                break;
            case GroundSide.Right:
                pivotPoint = transform.position + transform.right * offset + transform.up * direction * offset;
                break;
            case GroundSide.None:
                pivotPoint = transform.position;
                break;
        }

        float rotated = 0f;
        float totalAngle = 90f;

        while (rotated < totalAngle)
        {
            float rotateStep = (totalAngle / rollDuration) * Time.deltaTime;
            rotateStep = Mathf.Min(rotateStep, totalAngle - rotated);
            rotated += rotateStep;

            if (_currentGroundSide == GroundSide.None)
            {
                transform.Rotate(0f, 0f, rotateStep * direction);
            }
            else
            {
                transform.RotateAround(pivotPoint, Vector3.forward, rotateStep * direction);
            }

            yield return null;
        }

        float z = Mathf.Round(transform.eulerAngles.z / 90f) * 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, z);

        isRolling = false;
    }

    public void SetWallCling(bool value)
    {
        isWallClinging = value;

        if (rb != null)
        {
            rb.gravityScale = value ? 0f : originalGravity;
            if (value)
                rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    // ========== 已删除：Slippery 相关方法 ==========

    public bool IsOnFace(GameObject faceObj)
    {
        if (faceObj == null) return false;
        if (_currentGroundSide == GroundSide.Bottom && faceObj == bottomFace) return true;
        if (_currentGroundSide == GroundSide.Top && faceObj == topFace) return true;
        if (_currentGroundSide == GroundSide.Left && faceObj == leftFace) return true;
        if (_currentGroundSide == GroundSide.Right && faceObj == rightFace) return true;
        return false;
    }

    private GameObject GetFaceBySide(GroundSide side)
    {
        return side switch
        {
            GroundSide.Bottom => bottomFace,
            GroundSide.Top => topFace,
            GroundSide.Left => leftFace,
            GroundSide.Right => rightFace,
            _ => null
        };
    }

    private void UpdateActiveFaceAbility()
    {
        GameObject activeFace = GetFaceBySide(_currentGroundSide);
        if (activeFace == null) return;

        FaceAbilityBase[] abilities = activeFace.GetComponents<FaceAbilityBase>();
        foreach (var ability in abilities)
        {
            if (ability.isFaceActive)
            {
                ability.AbilityUpdate();
            }
        }
    }

    private void OnDestroy()
    {
        OnGroundSideChanged = null;
    }
}