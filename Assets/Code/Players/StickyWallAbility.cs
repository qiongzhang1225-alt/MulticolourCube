using UnityEngine;

public class StickyWallAbility : FaceAbilityBase
{
    [Header("ǽ����������")]
    [Tooltip("��ǽ�ƶ��ٶ�")] public float wallMoveSpeed = 3f;
    [Tooltip("ǽ����Ծ����")] public float wallJumpForce = 6f;
    [Tooltip("����ʱ�Ƿ���ȫ��������")] public bool zeroGravityOnCling = true;

    private Rigidbody2D rb;
    private bool isTouchingWall = false;
    private Collider2D currentWall;
    private float originalGravityScale;

    // ����������������������ǰ�漤�� + ȷʵ����ǽ
    private bool CanCling => isFaceActive && isTouchingWall;

    protected override void Awake()
    {
        base.Awake(); // ���ػ���
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
        ExitClinging(); // ��ʧЧʱ�����˳�����
    }

    public override void AbilityUpdate()
    {
        if (!CanCling) return;

        HandleWallMovement();
        HandleWallJump();
    }

    // ͳһ���������������޸��㣩
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
        if (InputAdapter.JumpPressed)
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