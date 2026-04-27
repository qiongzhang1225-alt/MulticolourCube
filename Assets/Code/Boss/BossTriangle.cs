using UnityEngine;
using System.Collections;

/// <summary>
/// 三角形 Boss 控制器：
///   - 三个面（红/黄/蓝），每隔一段时间旋转 120°，朝下面决定攻击模式
///   - 红：冲撞玩家，无视地形
///   - 黄：悬停在锚点，向四周发射可弹射子弹
///   - 蓝：朝玩家发射激光
///   - 10 点血量；玩家从顶部踩踏可造成 1 点伤害（推荐黄/蓝阶段）
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossTriangle : MonoBehaviour
{
    public enum BossPhase { Red, Yellow, Blue }

    // ══════════════════════════════════════
    //  血量
    // ══════════════════════════════════════
    [Header("血量")]
    public int maxHP = 10;
    public int currentHP;

    // ══════════════════════════════════════
    //  阶段
    // ══════════════════════════════════════
    [Header("阶段时长")]
    [Tooltip("每个阶段持续时间（秒）")] public float phaseDuration = 6f;
    [Tooltip("阶段之间旋转过渡时长")] public float rotationDuration = 0.5f;

    [Header("阶段颜色")]
    public Color redColor    = new Color(1f, 0.35f, 0.35f);
    public Color yellowColor = new Color(1f, 0.92f, 0.25f);
    public Color blueColor   = new Color(0.35f, 0.65f, 1f);

    // ══════════════════════════════════════
    //  红阶段 —— 冲撞
    // ══════════════════════════════════════
    [Header("红 — 冲撞")]
    [Tooltip("持续追踪玩家的速度")] public float chaseSpeed = 2.5f;
    [Tooltip("冲刺速度（远高于追踪速度）")] public float dashSpeed = 16f;
    [Tooltip("单次冲刺距离（米）")] public float dashDistance = 6f;
    [Tooltip("两次冲刺之间的间隔")] public float dashInterval = 1.8f;
    [Tooltip("冲刺前的蓄力时间（红光闪烁警示）")] public float dashTelegraphTime = 0.3f;

    // ══════════════════════════════════════
    //  黄阶段 —— 弹射子弹
    // ══════════════════════════════════════
    [Header("黄 — 子弹")]
    public GameObject bulletPrefab;
    [Tooltip("每轮发射的子弹数")] public int bulletCountPerVolley = 6;
    [Tooltip("两轮之间间隔")] public float bulletInterval = 1.0f;
    [Tooltip("子弹速度")] public float bulletSpeed = 6f;

    // ══════════════════════════════════════
    //  蓝阶段 —— 激光
    // ══════════════════════════════════════
    [Header("蓝 — 激光")]
    public GameObject laserPrefab;
    [Tooltip("激光蓄力时间（红色警示线）")] public float laserChargeTime = 0.8f;
    [Tooltip("激光持续时间")] public float laserActiveTime = 1.2f;
    [Tooltip("激光之间的冷却")] public float laserCooldown = 0.6f;
    [Tooltip("蓄力期追踪角速度（度/秒）。越小越笨拙——给玩家更多躲闪空间。-1 表示瞬间锁定（不推荐）")]
    public float laserTrackingSpeed = 120f;

    // ══════════════════════════════════════
    //  锚点 / 引用
    // ══════════════════════════════════════
    [Header("引用")]
    [Tooltip("黄/蓝阶段悬停的锚点（留空则用初始位置）")]
    public Transform anchorPoint;
    public BossHealthBar healthBar;

    [Header("活动范围")]
    [Tooltip("Boss 被限制在此 BoxCollider2D 范围内（红冲撞阶段会反弹，黄/蓝阶段悬停位置会被夹紧）。留空表示不限制。")]
    public BoxCollider2D arenaBounds;
    [Tooltip("冲撞阶段碰到边界时是否反弹（false 则贴边停止）")]
    public bool bounceOnArenaEdge = true;

    // ══════════════════════════════════════
    //  内部
    // ══════════════════════════════════════
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform player;
    private BossPhase currentPhase = BossPhase.Red;
    private Coroutine phaseRoutine;
    private bool invulnerable = false;
    private bool dead = false;
    private Vector3 startPos;
    private Vector2 chargeVelocity;

    // 默认 collider 是否为 trigger（保留原状）
    private bool defaultIsTrigger;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        defaultIsTrigger = col.isTrigger;

        // Boss 不受重力（自定义控制）
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        currentHP = maxHP;
        startPos = transform.position;
    }

    void Start()
    {
        // 用 PlayerColorSensor 组件锁定真正的玩家——避免被同样带 "Player" tag 的星星等物体误判
        var sensor = FindObjectOfType<PlayerColorSensor>();
        if (sensor != null)
        {
            player = sensor.transform;
        }
        else
        {
            var ps = GameObject.FindWithTag("Player");
            if (ps != null) player = ps.transform;
        }

        if (healthBar != null)
        {
            healthBar.SetMaxHP(maxHP);
            healthBar.SetHP(currentHP);
        }

        SwitchPhase(BossPhase.Red);
    }

    // ══════════════════════════════════════
    //  阶段切换
    // ══════════════════════════════════════

    void SwitchPhase(BossPhase next)
    {
        if (phaseRoutine != null) StopCoroutine(phaseRoutine);
        currentPhase = next;
        switch (next)
        {
            case BossPhase.Red:
                sr.color = redColor;
                phaseRoutine = StartCoroutine(RedPhase());
                break;
            case BossPhase.Yellow:
                sr.color = yellowColor;
                phaseRoutine = StartCoroutine(YellowPhase());
                break;
            case BossPhase.Blue:
                sr.color = blueColor;
                phaseRoutine = StartCoroutine(BluePhase());
                break;
        }
    }

    BossPhase NextPhase()
    {
        return (BossPhase)(((int)currentPhase + 1) % 3);
    }

    void TransitionToNextPhase()
    {
        if (dead) return;
        if (phaseRoutine != null) StopCoroutine(phaseRoutine);
        phaseRoutine = StartCoroutine(RotateAndSwitch());
    }

    IEnumerator RotateAndSwitch()
    {
        BossPhase next = NextPhase();

        // 旋转 120°（视觉上显示换面）
        float rotated = 0f;
        float total = 120f;
        while (rotated < total)
        {
            float step = total / rotationDuration * Time.deltaTime;
            step = Mathf.Min(step, total - rotated);
            transform.Rotate(0f, 0f, -step);
            rotated += step;
            // 颜色渐变
            sr.color = Color.Lerp(GetPhaseColor(currentPhase), GetPhaseColor(next), rotated / total);
            yield return null;
        }

        // 对齐到 120 的倍数
        float z = Mathf.Round(transform.eulerAngles.z / 120f) * 120f;
        transform.rotation = Quaternion.Euler(0f, 0f, z);

        SwitchPhase(next);
    }

    Color GetPhaseColor(BossPhase p)
    {
        switch (p)
        {
            case BossPhase.Red: return redColor;
            case BossPhase.Yellow: return yellowColor;
            case BossPhase.Blue: return blueColor;
        }
        return Color.white;
    }

    // ══════════════════════════════════════
    //  红阶段 —— 冲撞
    // ══════════════════════════════════════

    IEnumerator RedPhase()
    {
        // 无视地形：collider 切换为 trigger
        col.isTrigger = true;

        float elapsed = 0f;
        float dashTimer = dashInterval; // 第一次冲刺前的等待

        while (elapsed < phaseDuration && !dead)
        {
            // ── 慢速追踪玩家 ──
            if (player != null)
            {
                Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position);
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    Vector2 vel = toPlayer.normalized * chaseSpeed;
                    Vector3 next = transform.position + (Vector3)vel * Time.deltaTime;
                    next = ClampToArena(next, ref vel);
                    transform.position = next;
                }
            }

            // ── 周期性冲刺 ──
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f && elapsed < phaseDuration - 0.5f && !dead)
            {
                yield return DashOnce();
                dashTimer = dashInterval;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        col.isTrigger = defaultIsTrigger;
        TransitionToNextPhase();
    }

    /// <summary>突然朝玩家方向冲刺一段距离。蓄力期间高频闪烁警示。</summary>
    IEnumerator DashOnce()
    {
        if (player == null) yield break;

        // ── 蓄力警示（位置不动，颜色高频闪烁 + 轻微放大）──
        Color saved = sr.color;
        Vector3 savedScale = transform.localScale;

        float t = 0f;
        while (t < dashTelegraphTime)
        {
            // 高频闪白
            float k = Mathf.PingPong(t * 16f, 1f);
            sr.color = Color.Lerp(saved, Color.white, k);
            transform.localScale = savedScale * Mathf.Lerp(1f, 1.18f, t / dashTelegraphTime);
            t += Time.deltaTime;
            yield return null;
        }
        sr.color = saved;
        transform.localScale = savedScale;

        // ── 锁定方向 + 高速冲刺到指定距离（碰边按 bounce 规则反弹/停止）──
        Vector2 dashDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (dashDir.sqrMagnitude < 0.0001f) dashDir = Vector2.right;
        Vector2 dashVel = dashDir * dashSpeed;
        float traveled = 0f;

        while (traveled < dashDistance && !dead)
        {
            Vector3 step = (Vector3)dashVel * Time.deltaTime;
            float stepLen = step.magnitude;
            Vector3 next = transform.position + step;
            next = ClampToArena(next, ref dashVel);
            transform.position = next;
            traveled += stepLen;
            // 撞墙速度归零（不反弹时）就提前结束
            if (dashVel.sqrMagnitude < 0.01f) break;
            yield return null;
        }
    }

    /// <summary>
    /// 把目标位置夹紧到 arenaBounds 内。如果发生夹紧且 bounceOnArenaEdge 为 true，
    /// 则将对应轴上的速度反向（弹墙效果）。
    /// </summary>
    Vector3 ClampToArena(Vector3 target, ref Vector2 velocity)
    {
        if (arenaBounds == null) return target;

        Bounds b = arenaBounds.bounds;
        Vector3 clamped = target;
        bool hitX = false, hitY = false;

        if (clamped.x < b.min.x) { clamped.x = b.min.x; hitX = true; }
        else if (clamped.x > b.max.x) { clamped.x = b.max.x; hitX = true; }

        if (clamped.y < b.min.y) { clamped.y = b.min.y; hitY = true; }
        else if (clamped.y > b.max.y) { clamped.y = b.max.y; hitY = true; }

        if (bounceOnArenaEdge)
        {
            if (hitX) velocity.x = -velocity.x;
            if (hitY) velocity.y = -velocity.y;
        }
        else
        {
            if (hitX) velocity.x = 0f;
            if (hitY) velocity.y = 0f;
        }

        return clamped;
    }

    /// <summary>纯位置夹紧（无速度，悬停时使用）。</summary>
    Vector3 ClampPositionOnly(Vector3 target)
    {
        if (arenaBounds == null) return target;
        Bounds b = arenaBounds.bounds;
        return new Vector3(
            Mathf.Clamp(target.x, b.min.x, b.max.x),
            Mathf.Clamp(target.y, b.min.y, b.max.y),
            target.z);
    }

    // ══════════════════════════════════════
    //  黄阶段 —— 弹射子弹
    // ══════════════════════════════════════

    IEnumerator YellowPhase()
    {
        col.isTrigger = defaultIsTrigger;

        // 飞回锚点（确保在场地内）
        Vector3 anchor = anchorPoint != null ? anchorPoint.position : startPos;
        anchor = ClampPositionOnly(anchor);
        yield return MoveTo(anchor, 0.6f);

        float elapsed = 0f;
        while (elapsed < phaseDuration && !dead)
        {
            FireBulletVolley();
            yield return new WaitForSeconds(bulletInterval);
            elapsed += bulletInterval;
        }

        TransitionToNextPhase();
    }

    void FireBulletVolley()
    {
        if (bulletPrefab == null) return;

        // 根据 boss collider 半径确定子弹生成偏移，避免子弹生成在 boss 内部被自己撞反
        float spawnOffset = 0.5f;
        if (col != null) spawnOffset = col.bounds.extents.magnitude + 0.2f;

        float baseAngle = Random.Range(0f, 360f);
        for (int i = 0; i < bulletCountPerVolley; i++)
        {
            float angle = baseAngle + i * (360f / bulletCountPerVolley) + Random.Range(-12f, 12f);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            Vector3 spawnPos = transform.position + (Vector3)(dir * spawnOffset);
            var bulletGO = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

            // 双重保险：忽略 boss 与子弹之间的物理碰撞
            var bCol = bulletGO.GetComponent<Collider2D>();
            if (bCol != null && col != null)
                Physics2D.IgnoreCollision(bCol, col, true);

            var b = bulletGO.GetComponent<BossBullet>();
            if (b != null) b.Launch(dir * bulletSpeed);
            else
            {
                var rb2 = bulletGO.GetComponent<Rigidbody2D>();
                if (rb2 != null) rb2.velocity = dir * bulletSpeed;
            }
        }
    }

    // ══════════════════════════════════════
    //  蓝阶段 —— 激光
    // ══════════════════════════════════════

    IEnumerator BluePhase()
    {
        col.isTrigger = defaultIsTrigger;

        Vector3 anchor = anchorPoint != null ? anchorPoint.position : startPos;
        anchor = ClampPositionOnly(anchor);
        yield return MoveTo(anchor, 0.4f);

        float elapsed = 0f;
        float laserCycle = laserChargeTime + laserActiveTime + laserCooldown;
        while (elapsed < phaseDuration && !dead)
        {
            FireLaser();
            yield return new WaitForSeconds(laserCycle);
            elapsed += laserCycle;
        }

        TransitionToNextPhase();
    }

    void FireLaser()
    {
        if (laserPrefab == null || player == null) return;
        var laserGO = Instantiate(laserPrefab, transform.position, Quaternion.identity);
        var laser = laserGO.GetComponent<BossLaser>();
        if (laser != null)
        {
            // 让激光长度覆盖整个场地（取场地对角线 + 余量）
            if (arenaBounds != null)
            {
                Vector2 size = arenaBounds.bounds.size;
                laser.laserLength = size.magnitude + 4f;
            }
            // 应用追踪角速度（限制蓄力期对齐速度）
            laser.trackingDegreesPerSecond = laserTrackingSpeed;
            laser.Activate(transform, player, laserChargeTime, laserActiveTime);
        }
    }

    // ══════════════════════════════════════
    //  工具：移动到指定位置
    // ══════════════════════════════════════

    IEnumerator MoveTo(Vector3 target, float duration)
    {
        Vector3 from = transform.position;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, target, t / duration);
            yield return null;
        }
        transform.position = target;
    }

    // ══════════════════════════════════════
    //  伤害 / 死亡
    // ══════════════════════════════════════

    /// <summary>对 Boss 造成伤害（外部调用，例如玩家踩踏）。</summary>
    public void TakeDamage(int amount)
    {
        if (invulnerable || dead) return;
        currentHP -= amount;
        if (healthBar != null) healthBar.SetHP(currentHP);
        StartCoroutine(HitFlash());
        if (currentHP <= 0) Die();
    }

    IEnumerator HitFlash()
    {
        invulnerable = true;
        Color saved = sr.color;
        for (int i = 0; i < 3; i++)
        {
            sr.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            sr.color = saved;
            yield return new WaitForSeconds(0.05f);
        }
        invulnerable = false;
    }

    void Die()
    {
        dead = true;
        if (phaseRoutine != null) StopCoroutine(phaseRoutine);
        col.isTrigger = true;

        // 通知胜利（如果场景里有 VictoryUI）
        var victory = FindObjectOfType<VictoryUI>();
        if (victory != null) victory.ShowVictory();

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // 简单的死亡缩放+淡出
        float duration = 0.8f;
        Vector3 startScale = transform.localScale;
        Color startColor = sr.color;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, k);
            transform.Rotate(0f, 0f, 720f * Time.deltaTime);
            sr.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), k);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    // ══════════════════════════════════════
    //  碰撞处理
    // ══════════════════════════════════════

    /// <summary>
    /// 玩家碰到 Boss 即死亡（无论从哪个方向）。
    /// 红阶段下 collider 是 trigger，会走 OnTriggerEnter2D；其他阶段走 OnCollisionEnter2D。
    /// 踩踏伤害逻辑已移除——HP 只能通过外部调用 TakeDamage(int) 减少。
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (dead) return;
        if (collision.collider.CompareTag("Player"))
            KillPlayer(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;
        if (other.CompareTag("Player"))
            KillPlayer(other);
    }

    void KillPlayer(Collider2D playerCol)
    {
        var respawn = playerCol.GetComponent<PlayerRespawn>();
        if (respawn != null && !respawn.IsInvincible)
            respawn.Die();
    }

    // ══════════════════════════════════════
    //  公开访问
    // ══════════════════════════════════════
    public BossPhase CurrentPhase => currentPhase;
    public bool IsDead => dead;
}
