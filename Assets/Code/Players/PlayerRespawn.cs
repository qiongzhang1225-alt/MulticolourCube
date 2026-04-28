using UnityEngine;
using System;
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
    public float invincibleTime = 1.5f;
    public float blinkInterval = 0.15f;

    private SpriteRenderer[] renderers;
    private int checkpointOrder = -1;
    public bool IsInvincible { get; private set; } = false;

    // ──────── 死亡/复活特效事件（外部可订阅）────────
    /// <summary>玩家死亡时触发（参数：死亡位置）。可用于相机震动、粒子等。</summary>
    public event Action<Vector3> OnPlayerDeath;

    /// <summary>玩家复活完成后触发（参数：复活位置）。可用于光效、音效等。</summary>
    public event Action<Vector3> OnPlayerRespawned;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<BlockController>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        currentRespawnPoint = defaultRespawnPoint;
    }

    public void Die()
    {
        if (isRespawning || IsInvincible) return;
        // 询问 BossPlayerHP：这次是否致命？没有 HP 系统就当致命（兼容普通关卡）
        bool fatal = BossPlayerHP.Instance == null || BossPlayerHP.Instance.IsNextHitFatal();
        StartCoroutine(RespawnRoutine(fatal));
    }

    private IEnumerator RespawnRoutine(bool showDeathEffect)
    {
        isRespawning = true;
        controller.enabled = false;

        // 记录死亡位置（用于特效）
        Vector3 deathPos = transform.position;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 通知外部：玩家死亡（BossPlayerHP 在这里扣血）
        OnPlayerDeath?.Invoke(deathPos);

        // 隐藏玩家（即将进入黑幕）
        SetRenderersVisible(false);

        // ── 死亡黑幕：仅当致命时播放，软复活直接短暂停顿 ──
        if (showDeathEffect && DeathEffectUI.Instance != null)
        {
            yield return DeathEffectUI.Instance.PlayDeathSequence(deathPos);
        }
        else
        {
            yield return new WaitForSeconds(deathPause);
        }

        // ── 在黑幕（或短停顿）下执行重置 ──
        if (currentCheckpoint != null)
        {
            currentCheckpoint.ResetAllObjectStates();
        }

        // 复活位置
        Vector3 respawnPos = currentRespawnPoint != null
            ? currentRespawnPoint.position
            : defaultRespawnPoint != null
                ? defaultRespawnPoint.position
                : transform.position;

        transform.position = respawnPos;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;    // 防止移动平台导致的缩放异常
        Physics2D.SyncTransforms();

        // 恢复玩家显示
        SetRenderersVisible(true);

        // ── 黑幕淡出（仅致命死亡才有黑幕） ──
        if (showDeathEffect && DeathEffectUI.Instance != null)
        {
            yield return DeathEffectUI.Instance.PlayRespawnFadeOut(respawnPos);
        }

        // 通知外部：玩家已复活
        OnPlayerRespawned?.Invoke(respawnPos);

        // ── 无敌闪烁 ──
        yield return StartCoroutine(TemporaryInvincible());
        controller.enabled = true;
        isRespawning = false;
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (var sr in renderers) sr.enabled = visible;
    }

    private IEnumerator TemporaryInvincible()
    {
        IsInvincible = true;
        // 不禁用碰撞体，防止玩家穿透地面导致重复死亡
        // 仅依靠 IsInvincible 标记阻止 Die() 和各死亡源的伤害判定
        float t = 0;
        while (t < invincibleTime)
        {
            foreach (var sr in renderers) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }
        foreach (var sr in renderers) sr.enabled = true;
        IsInvincible = false;
    }

    public void UpdateCheckpoint(Transform point, int order, Checkpoint cp)
    {
        if (order > checkpointOrder)
        {
            checkpointOrder = order;
            currentRespawnPoint = point;
            currentCheckpoint = cp;
        }
    }
}
