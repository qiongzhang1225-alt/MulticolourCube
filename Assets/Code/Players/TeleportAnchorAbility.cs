using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class TeleportAnchorAbility : FaceAbilityBase
{
    public enum AbilityState { Idle, HasAnchor }

    [Header("�Ӵ��ж�")]
    [Tooltip("�������Ӿ��ο���ʵ�ʴ�����BlockController���漤��Ϊ׼")]
    public LayerMask surfaceLayers;

    [Header("��Ӱ����")]
    public bool showGhostPreview = true;
    [Range(0.1f, 0.6f)] public float ghostAlpha = 0.4f;
    public Color ghostColor = new Color(0.2f, 0.6f, 1f, 0.4f);

    [Header("��������")]
    public bool resetVelocityOnTeleport = true;
    [Tooltip("���ͺ���ݽ�����ײ�壨��ֹ��ǽ/��ɣ�")]
    public float disableCollisionTime = 0.15f;

    // �ڲ�״̬
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

        // ��ȡ�������ײ�����������RequireComponent��ʵ�ʲ�����⣩
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // ��ȡ������������
        rb = GetComponentInParent<Rigidbody2D>();
        parentCollider = GetComponentInParent<BoxCollider2D>();
        blockController = GetComponentInParent<BlockController>();

        if (rb == null || parentCollider == null)
        {
            Debug.LogError($"������ê������Ҳ��� Rigidbody2D �� BoxCollider2D��", this);
            enabled = false;
        }
    }

    protected override void OnAbilityEnable()
    {
        // �漤��ʱ���������Ӱ����ʾ
        if (ghostObject != null)
            ghostObject.SetActive(true);
    }

    protected override void OnAbilityDisable()
    {
        // ��ʧ��ʱ��������Ӱ���������ݣ�
        if (ghostObject != null)
            ghostObject.SetActive(false);
    }

    // ==================== ���ļ򻯣��Ƴ�������OnTrigger��� ====================
    // ��ȫ���� BlockController �� isFaceActive �ж�

    public override void AbilityUpdate()
    {
        // �������߼���ֻҪ�漤�� + ������Ҽ����ʹ�����
        // ��Ϊ BlockController �� isFaceActive = true ��������ζ�š������ӵ��ˡ�
        if (!isFaceActive || !InputAdapter.TeleportPressed)
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

    // ==================== ���±��ֲ��䣺�ȶ���ê���봫���߼� ====================

    private void SetAnchor()
    {
        // ֱ�Ӽ�¼�����壨��ɫ���壩�������������ת
        Transform parent = transform.parent;
        anchorWorldPosition = parent.position;
        anchorRotation = parent.rotation;

        currentState = AbilityState.HasAnchor;

        if (showGhostPreview)
        {
            CreateGhost();
        }

        Debug.Log($"��ê�������á���������: {anchorWorldPosition}");
    }

    private System.Collections.IEnumerator PerformTeleport()
    {
        Debug.Log($"����ʼ���͡�Ŀ��: {anchorWorldPosition}");
        Transform parent = transform.parent;

        // 1. ��ȫ������壬��ֹ��������
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.isKinematic = true;

        // 2. ���ݽ�������ײ��
        if (parentCollider != null)
            parentCollider.enabled = false;

        // 3. �ȴ�һ֡
        yield return null;

        // 4. ִ�д���
        parent.SetPositionAndRotation(anchorWorldPosition, anchorRotation);

        // 5. �ٵȴ�һ֡
        yield return null;

        // 6. �ָ�����ϵͳ
        rb.isKinematic = false;
        if (parentCollider != null)
            parentCollider.enabled = true;

        // 7. ˫�ر��������ٶ�
        if (resetVelocityOnTeleport)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 8. ����ê��
        ClearAnchor();

        Debug.Log($"��������ɡ���ǰλ��: {parent.position}");
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
            Debug.LogWarning("������ê�㡿�Ҳ��� SpriteRenderer���޷�������Ӱ��");
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