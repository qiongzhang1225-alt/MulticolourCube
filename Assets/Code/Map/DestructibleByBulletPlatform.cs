using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DestructibleByBulletPlatform : MonoBehaviour
{
    [Header("销毁设置")]
    public GameObject destroyEffectPrefab;     // 销毁特效（可选）
    public AudioClip destroySound;             // 销毁音效（可选）
    public float destroyDelay = 0.1f;          // 延迟销毁时间

    private AudioSource audioSource;

    private void Awake()
    {
        // 自动添加AudioSource（若需音效）
        if (destroySound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = destroySound;
            audioSource.playOnAwake = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 检测子弹碰撞（需给子弹预制体加Bullet标签）
        if (collision.collider.CompareTag("bullet"))
        {
            // 销毁子弹
            Destroy(collision.gameObject);
            // 销毁地块
            DestroyPlatform();
        }
    }

    /// <summary>
    /// 销毁地块逻辑
    /// </summary>
    private void DestroyPlatform()
    {
        // 播放音效
        if (audioSource != null) audioSource.Play();

        // 生成特效
        if (destroyEffectPrefab != null)
        {
            Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
        }

        // 禁用碰撞体
        GetComponent<Collider2D>().enabled = false;

        // 延迟销毁
        Destroy(gameObject, destroyDelay);
    }
}