using UnityEngine;

/// <summary>
/// 子弹：由炮台生成，碰到玩家触发死亡，碰到盾牌反弹。
/// 支持颜色着色——Turret 在生成后调用 SetBulletColor() 赋予颜色，
/// 子弹自动同步 SpriteRenderer + TrailRenderer（若有）的颜色。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("基础")]
    public float lifeTime = 10f;       // 子弹最长存活时间
    public int damage = 1;             // 伤害值（预留扩展）

    // ── 颜色 ──
    private Color bulletColor = Color.white;
    private SpriteRenderer sr;
    private TrailRenderer trail;

    /// <summary>子弹当前颜色（外部可读取）。</summary>
    public Color BulletColor => bulletColor;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        sr = GetComponent<SpriteRenderer>();
        trail = GetComponent<TrailRenderer>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// 设置子弹颜色（由 Turret 在 Instantiate 后调用）。
    /// 同步 SpriteRenderer 和 TrailRenderer（若存在）。
    /// </summary>
    public void SetBulletColor(Color color)
    {
        bulletColor = color;

        // 精灵着色
        if (sr != null)
            sr.color = color;

        // 拖尾着色
        if (trail != null)
        {
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }
    }

    // ──────── 碰撞 ────────

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 玩家检测
        if (collision.collider.CompareTag("Player"))
        {
            var respawn = collision.collider.GetComponent<PlayerRespawn>();
            if (respawn != null && !respawn.IsInvincible)
            {
                respawn.Die();
            }
            Destroy(gameObject);
            return;
        }

        // 盾牌反弹
        if (collision.collider.CompareTag("Shield"))
        {
            Vector2 incoming = rb.velocity;
            Vector2 normal = collision.contacts[0].normal;
            rb.velocity = Vector2.Reflect(incoming, normal);
            return;
        }

        // 其他碰撞（墙/地板）—— 保持原有物理行为
    }
}
