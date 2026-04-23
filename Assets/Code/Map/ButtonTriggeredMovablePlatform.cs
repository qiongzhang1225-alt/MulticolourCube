using UnityEngine;

/// <summary>
/// 按钮/颜色条件驱动的可移动平台。
/// 支持两种触发模式：
///   OneShot  —— 触发一次后永久启动 MovablePlatform（原有行为）
///   HoldToMove —— 持续触发时平滑滑向目标位置，释放后回到原位（开门效果）
/// 触发源可以是物理按钮 (ButtonTrigger) 和/或颜色条件组 (ColorConditionGroup)。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ButtonTriggeredMovablePlatform : MonoBehaviour, IResettable
{
    public enum TriggerMode
    {
        OneShot,      // 触发一次 → 永久启动 MovablePlatform
        HoldToMove,   // 持续触发 → 平滑移动到目标；释放 → 复位（开门/关门）
    }

    [Header("触发模式")]
    public TriggerMode triggerMode = TriggerMode.OneShot;

    [Header("触发源：物理按钮（可选）")]
    [Tooltip("拖入场景中的按钮物体，需要 ButtonTrigger 脚本")]
    public GameObject triggerButton;
    public float triggerDelay = 0.2f;

    [Header("触发源：颜色条件组（可选）")]
    [Tooltip("拖入 ColorConditionGroup 物体，所有颜色条件满足时视为按钮激活")]
    public ColorConditionGroup colorConditionGroup;

    [Header("HoldToMove 参数（仅 HoldToMove 模式使用）")]
    [Tooltip("移动方向（本地坐标）")]
    public Vector2 holdMoveDirection = Vector2.up;
    [Tooltip("移动速度")]
    [Min(0.1f)] public float holdMoveSpeed = 3f;
    [Tooltip("最大移动距离")]
    [Min(0.1f)] public float holdMoveDistance = 3f;

    // ── 内部引用 ──
    private MovablePlatform movablePlatform;
    private ButtonTrigger buttonTrigger;
    private Rigidbody2D rb;

    // ── OneShot 状态 ──
    private bool oneShotFired = false;

    // ── HoldToMove 状态 ──
    private Vector3 closedPos;   // 关闭位置（初始）
    private Vector3 openPos;     // 开启位置
    private bool buttonActive = false;
    private bool colorGroupActive = false;
    private bool IsAnySourceActive => buttonActive || colorGroupActive;

    // ── 跟随系统（替代 SetParent，避免旋转变形）──
    private readonly System.Collections.Generic.List<Transform> riders =
        new System.Collections.Generic.List<Transform>();

    // ──────────────────────────────────────
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movablePlatform = GetComponent<MovablePlatform>();

        // ── OneShot 初始化 ──
        if (triggerMode == TriggerMode.OneShot)
        {
            if (movablePlatform != null) movablePlatform.enabled = false;
        }

        // ── HoldToMove 初始化 ──
        if (triggerMode == TriggerMode.HoldToMove)
        {
            closedPos = transform.position;
            openPos = closedPos + (Vector3)(holdMoveDirection.normalized * holdMoveDistance);

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;

            // HoldToMove 不需要 MovablePlatform，禁用以避免冲突
            if (movablePlatform != null) movablePlatform.enabled = false;
        }

        // ── 绑定物理按钮 ──
        if (triggerButton != null)
        {
            buttonTrigger = triggerButton.GetComponent<ButtonTrigger>();
            if (buttonTrigger == null)
            {
                buttonTrigger = triggerButton.AddComponent<ButtonTrigger>();
                Debug.LogWarning("[ButtonPlatform] 按钮缺少 ButtonTrigger，已自动添加");
            }
            buttonTrigger.OnButtonTriggered += OnButtonActivated;
            buttonTrigger.OnButtonReleased  += OnButtonDeactivated;
        }

        // ── 绑定颜色条件组 ──
        if (colorConditionGroup != null)
        {
            colorConditionGroup.OnAllConditionsMet += OnColorGroupActivated;
            colorConditionGroup.OnConditionBroken  += OnColorGroupDeactivated;
        }
    }

    // ──────── 触发源回调 ────────

    private void OnButtonActivated()
    {
        buttonActive = true;
        HandleTriggerStateChange();
    }

    private void OnButtonDeactivated()
    {
        buttonActive = false;
        HandleTriggerStateChange();
    }

    private void OnColorGroupActivated()
    {
        colorGroupActive = true;
        HandleTriggerStateChange();
    }

    private void OnColorGroupDeactivated()
    {
        colorGroupActive = false;
        HandleTriggerStateChange();
    }

    /// <summary>任一触发源状态变化后统一处理。</summary>
    private void HandleTriggerStateChange()
    {
        switch (triggerMode)
        {
            case TriggerMode.OneShot:
                if (IsAnySourceActive && !oneShotFired)
                {
                    oneShotFired = true;
                    Invoke(nameof(FireOneShot), triggerDelay);
                }
                break;

            case TriggerMode.HoldToMove:
                // HoldToMove 模式由 Update 驱动，这里无需额外操作
                Debug.Log("[ButtonPlatform] HoldToMove " +
                    (IsAnySourceActive ? "开启" : "关闭") + " → " + gameObject.name);
                break;
        }
    }

    private void FireOneShot()
    {
        if (movablePlatform != null)
        {
            movablePlatform.enabled = true;
            Debug.Log("[ButtonPlatform] OneShot 启动移动：" + gameObject.name);
        }
    }

    // ──────── HoldToMove 运动 ────────

    private void Update()
    {
        if (triggerMode != TriggerMode.HoldToMove) return;

        Vector3 prevPos = transform.position;
        Vector3 target = IsAnySourceActive ? openPos : closedPos;

        if (Vector3.Distance(transform.position, target) > 0.005f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, holdMoveSpeed * Time.deltaTime);
        }

        // 位移增量带动跟随者（替代 SetParent）
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

    // ── HoldToMove 模式下的玩家跟随注册 ──
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggerMode == TriggerMode.HoldToMove && collision.collider.CompareTag("Player"))
        {
            Transform t = collision.collider.transform;
            if (!riders.Contains(t))
                riders.Add(t);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (triggerMode == TriggerMode.HoldToMove && collision.collider.CompareTag("Player"))
        {
            riders.Remove(collision.collider.transform);
        }
    }

    // ──────── 清理 & 重置 ────────

    private void OnDestroy()
    {
        if (buttonTrigger != null)
        {
            buttonTrigger.OnButtonTriggered -= OnButtonActivated;
            buttonTrigger.OnButtonReleased  -= OnButtonDeactivated;
        }
        if (colorConditionGroup != null)
        {
            colorConditionGroup.OnAllConditionsMet -= OnColorGroupActivated;
            colorConditionGroup.OnConditionBroken  -= OnColorGroupDeactivated;
        }
    }

    /// <summary>重置平台到初始状态（关卡重置 / 检查点恢复时调用）。</summary>
    public void ResetPlatform()
    {
        oneShotFired = false;
        buttonActive = false;
        colorGroupActive = false;
        riders.Clear();

        if (triggerMode == TriggerMode.HoldToMove)
            transform.position = closedPos;

        if (movablePlatform != null)
            movablePlatform.enabled = false;
        if (buttonTrigger != null)
            buttonTrigger.ResetButton();
    }

    // ── IResettable ──
    private bool _savedOneShotFired;
    private Vector3 _savedPosition;

    public void SaveCheckpointState()
    {
        _savedOneShotFired = oneShotFired;
        _savedPosition = transform.position;
    }

    public void ResetToCheckpointState()
    {
        oneShotFired = _savedOneShotFired;
        buttonActive = false;
        colorGroupActive = false;
        riders.Clear();
        transform.position = _savedPosition;

        // 如果存档时尚未触发，禁用 MovablePlatform
        if (!_savedOneShotFired && movablePlatform != null)
            movablePlatform.enabled = false;
    }
}