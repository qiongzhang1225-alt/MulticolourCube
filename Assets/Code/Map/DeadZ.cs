using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DeathZone : MonoBehaviour
{
    private void Reset()
    {
        // 确保是触发器
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var respawn = other.GetComponent<PlayerRespawn>();
        if (respawn != null)
        {
            // 只有在非无敌状态下才触发死亡
            if (!respawn.IsInvincible)
            {
                respawn.Die();
            }
        }
    }
}
