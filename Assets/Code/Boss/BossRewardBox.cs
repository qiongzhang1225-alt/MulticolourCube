using UnityEngine;

/// <summary>
/// Boss 关卡 — 奖励箱（白色方块占位）。
///   小球被激活后掉落。玩家碰到则"开箱"——按 bombChance 概率掉炸弹，否则掉爱心。
///   开出的物品在原位生成；箱子销毁。
///
/// 注：玩家用任意面碰即可开箱（不要求颜色匹配）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossRewardBox : MonoBehaviour
{
    [Header("开箱概率")]
    [Tooltip("掉落炸弹的概率（0..1）；其余概率掉爱心")]
    [Range(0f, 1f)] public float bombChance = 0.5f;

    [Header("奖励 Prefab")]
    public GameObject bombPrefab;
    public GameObject heartPrefab;

    [Header("生命周期")]
    [Tooltip("最长存活时间（秒），超时自动销毁防积压")]
    public float lifeTime = 15f;

    private bool opened = false;

    void Start()
    {
        if (lifeTime > 0f) Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter2D(Collision2D collision) => TryOpen(collision.collider);
    void OnTriggerEnter2D(Collider2D other) => TryOpen(other);

    void TryOpen(Collider2D other)
    {
        if (opened) return;
        if (!other.CompareTag("Player")) return;

        // 区分真正的玩家（带 PlayerColorSensor），避免 star 等共享 Player tag 的对象误开
        if (other.GetComponent<PlayerColorSensor>() == null) return;

        opened = true;
        SpawnReward();
        Destroy(gameObject);
    }

    void SpawnReward()
    {
        bool bomb = Random.value < bombChance;
        GameObject prefab = bomb ? bombPrefab : heartPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[BossRewardBox] 缺少 {(bomb ? "bombPrefab" : "heartPrefab")} 引用");
            return;
        }
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
