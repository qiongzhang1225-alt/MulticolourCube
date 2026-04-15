using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BreakableFromBelowPlatform : MonoBehaviour
{
    [Header("撞碎设置")]
    public string triggerTag = "Player";       // 可撞碎的对象标签
    public GameObject breakEffectPrefab;       // 撞碎特效（可选）
    public float destroyDelay = 0.1f;          // 撞后延迟销毁时间

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag(triggerTag)) return;

        // 判断是否从下方撞击：碰撞接触点的法线向上（地块的下表面被撞）
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.8f) // 法线y轴>0.8=近似向上（容错）
            {
                BreakPlatform();
                break;
            }
        }
    }

    /// <summary>
    /// 销毁地块并生成特效
    /// </summary>
    private void BreakPlatform()
    {
        // 生成撞碎特效
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        // 禁用碰撞体（避免二次触发）
        GetComponent<Collider2D>().enabled = false;

        // 延迟销毁（兼容特效播放）
        Destroy(gameObject, destroyDelay);
    }
}