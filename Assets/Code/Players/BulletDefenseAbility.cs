using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class BulletDefenseAbility : FaceAbilityBase
{
    public enum DefenseMode
    {
        Immune,
        Reflect
    }

    [Header("核心模式")]
    public DefenseMode defenseMode = DefenseMode.Reflect;

    [Header("✅ 手动强制激活此面")]
    public bool forceFaceActive = true; // 你手动控制激活

    [Header("碰撞体")]
    public Collider2D mainCollider;
    public Collider2D bulletDetectorCollider;

    [Header("子弹设置")]
    public string enemyBulletTag = "bullet";
    public float reflectMultiplier = 1.2f;

    private bool originalIsTrigger;
    private string originalTag;
    private bool isShieldActive = false;

    protected override void Awake()
    {
        base.Awake();
        originalIsTrigger = mainCollider.isTrigger;
        originalTag = gameObject.tag;
    }

    // ======================================================================
    // 【关键】完全重写面激活逻辑 → 手动控制
    // ======================================================================
    public override void OnFaceActivated()
    {
        // 不使用基类的自动激活
        isFaceActive = forceFaceActive;
    }

    public override void OnFaceDeactivated()
    {
        // 不使用基类的自动失活
        isFaceActive = forceFaceActive;
    }

    private void Update()
    {
        // 强制让 isFaceActive 等于你手动设置的值
        isFaceActive = forceFaceActive;

        // 预警碰撞体始终开启
        if (bulletDetectorCollider != null)
            bulletDetectorCollider.enabled = true;

        // 手动关闭时关闭护盾
        if (!forceFaceActive && isShieldActive)
            DeactivateShield();
    }

    // ======================================================================
    // 子弹检测
    // ======================================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!forceFaceActive) return;
        if (!other.CompareTag(enemyBulletTag)) return;

        ActivateShield();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(enemyBulletTag)) return;
        DeactivateShield();
    }

    // ======================================================================
    // 护盾开关（切换 isTrigger）
    // ======================================================================
    void ActivateShield()
    {
        isShieldActive = true;
        gameObject.tag = "Shield";
        mainCollider.isTrigger = false;
        Debug.Log("✅ 护盾已激活 → 变成墙壁");
    }

    void DeactivateShield()
    {
        isShieldActive = false;
        gameObject.tag = originalTag;
        mainCollider.isTrigger = originalIsTrigger;
        Debug.Log("✅ 护盾已关闭 → 恢复颜色检测");
    }

    // ======================================================================
    // 反弹
    // ======================================================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isShieldActive) return;
        if (!collision.collider.CompareTag(enemyBulletTag)) return;

        Rigidbody2D rb = collision.collider.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 dir = Vector2.Reflect(rb.velocity, collision.contacts[0].normal);
        rb.velocity = dir * reflectMultiplier;
    }

    protected override void OnAbilityEnable() { }
    protected override void OnAbilityDisable() { }
}