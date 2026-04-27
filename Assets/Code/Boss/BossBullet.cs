using UnityEngine;

/// <summary>
/// Boss 弹射子弹：碰墙反弹（保持原速度），碰玩家致死，存活时间到期销毁。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossBullet : MonoBehaviour
{
    [Header("基础")]
    [Tooltip("最长存活时间（秒）")] public float lifeTime = 5f;
    [Tooltip("最大反弹次数（超过则销毁）")] public int maxBounces = 4;

    [Header("视觉")]
    [Tooltip("可选：拖尾颜色")] public bool useTrailColor = true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private TrailRenderer trail;
    private int bounceCount = 0;
    private float speedTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        sr = GetComponent<SpriteRenderer>();
        trail = GetComponent<TrailRenderer>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>外部调用：设置子弹速度（方向 × 速度）。</summary>
    public void Launch(Vector2 velocity)
    {
        rb.velocity = velocity;
        speedTarget = velocity.magnitude;

        // 朝向旋转
        if (velocity.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 玩家：致死
        if (collision.collider.CompareTag("Player"))
        {
            var respawn = collision.collider.GetComponent<PlayerRespawn>();
            if (respawn != null && !respawn.IsInvincible)
                respawn.Die();
            Destroy(gameObject);
            return;
        }

        // 其他：反弹
        bounceCount++;
        if (bounceCount > maxBounces)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 incoming = rb.velocity;
        Vector2 normal = collision.contacts[0].normal;
        Vector2 reflected = Vector2.Reflect(incoming, normal).normalized * speedTarget;
        rb.velocity = reflected;

        // 朝向更新
        float angle = Mathf.Atan2(reflected.y, reflected.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
