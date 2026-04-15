using UnityEngine;

public class StickyWallAbility : FaceAbilityBase
{
    [Header("墙面吸附设置")]
    [Tooltip("沿墙移动速度")] public float wallMoveSpeed = 3f;
    [Tooltip("墙面跳跃力度")] public float wallJumpForce = 6f;
    [Tooltip("吸附时是否完全抵消重力")] public bool zeroGravityOnCling = true;

    private Rigidbody2D rb;
    private bool isTouchingWall = false;
    private Collider2D currentWall;
    private float originalGravityScale;

    // 真正允许吸附的条件：当前面激活 + 确实碰到墙
    private bool CanCling => isFaceActive && isTouchingWall;

    protected override void Awake()
    {
        base.Awake(); // 遵守基类
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb != null)
            originalGravityScale = rb.gravityScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            isTouchingWall = true;
            currentWall = other;
            UpdateClingState();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            isTouchingWall = false;
            currentWall = null;
            UpdateClingState();
        }
    }

    protected override void OnAbilityEnable()
    {
        UpdateClingState();
    }

    protected override void OnAbilityDisable()
    {
        ExitClinging(); // 面失效时必须退出吸附
    }

    public override void AbilityUpdate()
    {
        if (!CanCling) return;

        HandleWallMovement();
        HandleWallJump();
    }

    // 统一开关吸附（核心修复点）
    private void UpdateClingState()
    {
        if (CanCling)
            EnterClinging();
        else
            ExitClinging();
    }

    private void EnterClinging()
    {
        if (zeroGravityOnCling)
        {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(rb.velocity.x, 0f);
        }
    }

    private void ExitClinging()
    {
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale;
        }
    }

    private void HandleWallMovement()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        rb.velocity = new Vector2(rb.velocity.x, verticalInput * wallMoveSpeed);
    }

    private void HandleWallJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2 jumpDir = GetJumpDirectionByGroundSide();

            if (jumpDir == Vector2.zero && currentWall != null)
            {
                jumpDir = (transform.position - currentWall.transform.position).normalized;
            }

            if (jumpDir != Vector2.zero)
            {
                ExitClinging();
                rb.velocity = jumpDir * wallJumpForce;
            }
        }
    }

    private Vector2 GetJumpDirectionByGroundSide()
    {
        if (blockController == null) return Vector2.zero;

        return blockController.currentGroundSide switch
        {
            BlockController.GroundSide.Left => transform.right,
            BlockController.GroundSide.Right => -transform.right,
            BlockController.GroundSide.Top => -transform.up,
            BlockController.GroundSide.Bottom => transform.up,
            _ => Vector2.zero
        };
    }
}