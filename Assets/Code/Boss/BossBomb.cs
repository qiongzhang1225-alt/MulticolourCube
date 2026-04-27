using UnityEngine;

/// <summary>
/// Boss 关卡 — 追踪导弹：从奖励箱开出后立即飞向 BossTriangle，命中造成 damage 伤害后自爆。
/// 寻敌策略：优先 FindObjectOfType&lt;BossTriangle&gt;()。
/// 飞行：默认无重力 + 立即追踪 + 高加速度，行为像导弹（也可以把 homingDelay/initialUpSpeed/gravity 调高，
/// 让它表现得像弧线投掷的"炸弹"）。
/// 命中判定：触发器（Trigger）模式 — 与 Boss 的 Collider 重叠即命中。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossBomb : MonoBehaviour
{
    [Header("伤害")]
    public int damage = 2;

    [Header("飞行")]
    [Tooltip("初始上抛速度（导弹一般填 0；炸弹弧线则填 4 左右）")]
    public float initialUpSpeed = 0f;
    [Tooltip("最大飞行速度")] public float maxSpeed = 14f;
    [Tooltip("加速度（单位/秒²），值越大转向越灵活；导弹建议 25~40")]
    public float homingAccel = 30f;
    [Tooltip("起步追踪前的延迟。导弹填 0 = 立即锁敌；炸弹可填 0.2~0.4 让它先弹一下")]
    public float homingDelay = 0f;
    [Tooltip("起步重力。导弹建议 0；炸弹建议 0.5~1 模拟弧线掉落")]
    public float startGravity = 0f;

    [Header("生命")]
    public float lifeTime = 8f;

    [Header("视觉")]
    public SpriteRenderer sr;

    private Rigidbody2D rb;
    private BossTriangle boss;
    private float age;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = startGravity;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;              // 触发器模式：穿过墙体直接撞 Boss

        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // 起手速度：导弹直接朝 Boss 方向（若已找到），炸弹则上抛
        boss = FindObjectOfType<BossTriangle>();
        if (initialUpSpeed > 0.001f)
        {
            rb.velocity = new Vector2(Random.Range(-1f, 1f), initialUpSpeed);
        }
        else if (boss != null)
        {
            Vector2 dir = ((Vector2)boss.transform.position - (Vector2)transform.position).normalized;
            rb.velocity = dir * Mathf.Min(maxSpeed, 6f);
        }
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (age < homingDelay) return;
        if (boss == null) { boss = FindObjectOfType<BossTriangle>(); return; }

        // 关闭重力进入追踪
        if (rb.gravityScale != 0f) rb.gravityScale = 0f;

        Vector2 toBoss = (Vector2)boss.transform.position - (Vector2)transform.position;
        if (toBoss.sqrMagnitude < 0.0001f) return;
        Vector2 desired = toBoss.normalized * maxSpeed;
        Vector2 newVel = Vector2.MoveTowards(rb.velocity, desired, homingAccel * Time.fixedDeltaTime);
        rb.velocity = newVel;

        // 朝向旋转
        if (newVel.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(newVel.y, newVel.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var bt = other.GetComponentInParent<BossTriangle>();
        if (bt != null)
        {
            bt.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
