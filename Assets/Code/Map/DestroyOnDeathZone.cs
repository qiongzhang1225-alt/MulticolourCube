using UnityEngine;

/// <summary>
/// 挂载在石头（或其他物体）上。
/// 当物体进入死亡区域（带有 DeathZone 组件的触发器）时，立刻销毁自身。
/// </summary>
public class DestroyOnDeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检测碰撞的对象是否带有 DeathZone 组件
        if (other.TryGetComponent<DeathZone>(out _))
        {
            Destroy(gameObject);
        }
    }
}