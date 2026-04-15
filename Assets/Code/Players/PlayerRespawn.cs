using UnityEngine;
using System.Collections;

public class PlayerRespawn : MonoBehaviour
{
    public Transform defaultRespawnPoint;
    private Transform currentRespawnPoint;
    private Checkpoint currentCheckpoint;
    private Rigidbody2D rb;
    private BlockController controller;
    private bool isRespawning = false;

    public float deathPause = 0.2f;
    public float invincibleTime = 0.5f;
    public float blinkInterval = 0.15f;

    private SpriteRenderer[] renderers;
    private Collider2D[] playerColliders;
    private int checkpointOrder = -1;
    public bool IsInvincible { get; private set; } = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<BlockController>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        playerColliders = GetComponentsInChildren<Collider2D>();
        currentRespawnPoint = defaultRespawnPoint;
    }

    public void Die()
    {
        if (!isRespawning && !IsInvincible) StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        controller.enabled = false;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        yield return new WaitForSeconds(deathPause);

        // ========== 关键日志：看是否执行了场景重置 ==========
        if (currentCheckpoint != null)
        {
            Debug.Log("【玩家】复活，通知检查点重置场景", this);
            currentCheckpoint.ResetAllObjectStates();
        }
        else
        {
            Debug.LogError("【错误】没有绑定任何检查点！", this);
        }

        if (currentRespawnPoint != null)
        {
            transform.position = currentRespawnPoint.position;
            transform.rotation = Quaternion.identity;
            Physics2D.SyncTransforms();
        }

        yield return StartCoroutine(TemporaryInvincible());
        controller.enabled = true;
        isRespawning = false;
    }

    private IEnumerator TemporaryInvincible()
    {
        IsInvincible = true;
        SetColliders(false);
        float t = 0;
        while (t < invincibleTime)
        {
            foreach (var sr in renderers) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }
        foreach (var sr in renderers) sr.enabled = true;
        SetColliders(true);
        IsInvincible = false;
    }

    private void SetColliders(bool state)
    {
        foreach (var col in playerColliders) if (!col.isTrigger) col.enabled = state;
    }

    public void UpdateCheckpoint(Transform point, int order, Checkpoint cp)
    {
        if (order > checkpointOrder)
        {
            checkpointOrder = order;
            currentRespawnPoint = point;
            currentCheckpoint = cp;
            Debug.Log("【玩家】成功绑定新检查点！", this);
        }
    }
}