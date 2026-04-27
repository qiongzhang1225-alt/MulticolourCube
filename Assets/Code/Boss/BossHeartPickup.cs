using UnityEngine;

/// <summary>
/// Boss 关卡 — 爱心拾取：玩家碰到 +1 HP（通过 BossPlayerHP.Heal）。
/// 落下后允许停在地上等玩家来取；超时自动销毁防积压。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossHeartPickup : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("回血量")] public int healAmount = 1;
    [Tooltip("最长存活秒数；<=0 永久")] public float lifeTime = 12f;

    [Header("视觉")]
    [Tooltip("可选：拾取时的特效（可留空）")]
    public GameObject pickupVfx;

    private bool consumed = false;

    void Start()
    {
        if (lifeTime > 0f) Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter2D(Collision2D collision) => TryConsume(collision.collider);
    void OnTriggerEnter2D(Collider2D other) => TryConsume(other);

    void TryConsume(Collider2D other)
    {
        if (consumed) return;
        if (!other.CompareTag("Player")) return;
        // 真正的玩家
        if (other.GetComponent<PlayerColorSensor>() == null) return;

        consumed = true;
        if (BossPlayerHP.Instance != null)
            BossPlayerHP.Instance.Heal(healAmount);

        if (pickupVfx != null)
            Instantiate(pickupVfx, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
