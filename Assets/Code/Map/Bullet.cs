using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("参数")]
    public float lifeTime = 10f;       // 子弹存在的最长时间
    public int damage = 1;             // 击中玩家直接死亡即可

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;          // 默认不受重力影响（子弹直线飞行）
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        // 生命时间结束自动销毁
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //  玩家检测
        if (collision.collider.CompareTag("Player"))
        {
            var respawn = collision.collider.GetComponent<PlayerRespawn>();
            if (respawn != null && !respawn.IsInvincible)
            {
                respawn.Die();
            }

            Destroy(gameObject); // 击中玩家后消失
            return;
        }

        if (collision.collider.CompareTag("Shield"))
        {
            Vector2 incoming = rb.velocity;
            Vector2 normal = collision.contacts[0].normal;
            rb.velocity = Vector2.Reflect(incoming, normal);
            return; // 不销毁
        }

        //  其他情况（比如墙/地板）
        // 不销毁，交给物理引擎处理（反弹/停下）
        // 如果你希望它有反弹效果，可以调 Rigidbody2D 的 Physics Material2D
    }
}
