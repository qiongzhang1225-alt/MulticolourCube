using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Boss 关卡 — 双区域解谜节奏控制器：
///
///   场上分成 **左右两个区域**，每区 4 块 ColorChangePlatform：
///     - 其中 3 块玩家可踩到（用对应面色变色）
///     - 1 块玩家够不到 —— 必须用空中掉落的颜色球去碰才能变色
///
///   每回合：
///     1. 给左右两边各随机 4 个目标颜色
///     2. 全 8 个颜色推到 BossSuggestUI（前 4 = 左，后 4 = 右）
///     3. 随机选定本回合**只解一边**（active side）
///     4. 在 ballSpawnPoint 生成颜色球，颜色 = active 边那个"够不到平台"的目标色
///        → 玩家观察球色 → 推断要解哪边 → 把球弹到对应位置 + 自己踩 3 块
///     5. 监听 active 边 ColorConditionGroup.OnAllConditionsMet
///        触发时在 rewardDropPoint 上空生成奖励箱，进入下一回合
///
///   设计点：
///     - 球只生成 1 个；玩家如果把球弄丢，等下一回合
///     - 非 active 边玩家可以随便折腾，不影响判定
///     - "球色决定方位"是核心信息：球落到错误一侧也无效
/// </summary>
public class BossArenaController : MonoBehaviour
{
    [System.Serializable]
    public class RegionConfig
    {
        [Tooltip("此区域内的 4 个 ColorChangePlatform（顺序对应 BossSuggestUI 中本区域 4 格的从左到右）")]
        public List<ColorChangePlatform> platforms = new List<ColorChangePlatform>();

        [Tooltip("汇总本区域 4 块的 ColorConditionGroup")]
        public ColorConditionGroup group;

        [Tooltip("本区域中『玩家够不到、必须靠球触发』的平台索引（0~3）")]
        public int ballPlatformIndex = 0;

        [Tooltip("本区域专属的奖励箱掉落点（建议放在本区上空）")]
        public Transform rewardDropPoint;
    }

    [Header("两个区域（左 / 右）")]
    public RegionConfig leftRegion = new RegionConfig();
    public RegionConfig rightRegion = new RegionConfig();

    [Header("UI")]
    [Tooltip("BossSuggestUI；cubeImages 长度需为 8（前 4 = 左，后 4 = 右）")]
    public BossSuggestUI suggestUI;

    [Header("颜色球")]
    [Tooltip("颜色球 prefab（推荐拖入 ColorBall.prefab）—— 必须挂 BallColorCarrier + Rigidbody2D + Collider2D")]
    public GameObject ballPrefab;

    [Tooltip("颜色球出生点（如果未填 ballSpawnZone，则使用此点；都填则优先用 zone 随机点）")]
    public Transform ballSpawnPoint;

    [Tooltip("可选：颜色球从此 RainZone 区域内的随机点掉落（覆盖 ballSpawnPoint）")]
    public BossRainZone ballSpawnZone;

    [Header("奖励箱")]
    public GameObject rewardBoxPrefab;
    [Tooltip("当区域未指定 rewardDropPoint 时使用的备用掉落点")]
    public Transform fallbackRewardDropPoint;

    [Header("回合节奏")]
    [Tooltip("开局延迟（秒）。设 0 则进 Play 立即开始第一回合。")]
    public float startupDelay = 0.3f;
    [Tooltip("解出后等几秒再开下一回合")] public float interRoundDelay = 2.5f;
    [Tooltip("每回合最大持续时间（秒）；<=0 表示无超时")]
    public float roundTimeout = 0f;
    [Tooltip("是否允许同一区域内 4 个目标颜色重复")]
    public bool allowDuplicateColors = true;

    [Header("颜色球自动刷新（卡球时重出）")]
    [Tooltip("启用：当球被推到玩家拿不回的位置（卡墙缝、卡角落、出界等），到时自动销毁并重新生成")]
    public bool autoRefreshStuckBall = true;
    [Tooltip("球速度低于此阈值即视为可能卡住（单位/秒）。建议 0.05~0.2")]
    public float ballStuckSpeedThreshold = 0.1f;
    [Tooltip("低速持续多少秒后真正判定为卡死并重生（避免误判刚落地的瞬间静止）")]
    public float ballStuckGraceTime = 4f;
    [Tooltip("球刚生成后多久内不做卡死检测（让它自由落体到位）")]
    public float ballSpawnImmunity = 1.5f;
    [Tooltip("看门狗轮询间隔（秒）")]
    public float ballWatchdogInterval = 0.5f;

    [Header("视觉")]
    [Tooltip("非 active 区域的 UI 提示色叠加（变暗以提示当前不需要解）。"
        + "Alpha < 1 用于半透；设为白色不变暗")]
    public Color inactiveSideTint = new Color(0.75f, 0.75f, 0.75f, 1f);
    [Tooltip("是否使用 inactiveSideTint 把非 active 边变暗。默认关闭——"
        + "玩家应该靠球色判断解哪边，而不是靠 UI 暗示")]
    public bool dimInactiveSide = false;

    [Header("引用（可空，自动查找）")]
    public PlayerColorSensor colorSensor;

    [Header("调试")]
    public bool verboseLog = false;

    // ── 运行时 ──
    private GameObject currentBall;
    private bool roundSolved;
    private bool activeIsLeft;
    private RegionConfig activeRegion => activeIsLeft ? leftRegion : rightRegion;
    private float roundStartTime;
    private int roundNumber = 0;

    // 球看门狗状态
    private float currentBallSpawnTime;
    private float currentBallStuckTimer;

    // 缓存两边当前目标 face，方便只刷新一侧
    private List<CubeFace> leftFaces = new List<CubeFace>();
    private List<CubeFace> rightFaces = new List<CubeFace>();

    void Start()
    {
        if (colorSensor == null) colorSensor = FindObjectOfType<PlayerColorSensor>();
        StartCoroutine(MainLoop());
        StartCoroutine(BallWatchdog());
    }

    IEnumerator MainLoop()
    {
        if (startupDelay > 0f) yield return new WaitForSeconds(startupDelay);

        // 首次：完整初始化两边 + 选 active + 出球
        RandomizeSide(true);
        RandomizeSide(false);
        PushAllUIColors();
        activeIsLeft = (Random.value < 0.5f);
        SubscribeActive();
        SpawnBallForActiveSide();

        while (true)
        {
            // 等待 active 边解出 / 超时
            roundSolved = false;
            roundStartTime = Time.time;
            while (!roundSolved)
            {
                if (suggestUI != null && roundTimeout > 0f)
                {
                    float remaining = roundTimeout - (Time.time - roundStartTime);
                    suggestUI.SetTimerFill(Mathf.Clamp01(remaining / roundTimeout));
                    if (remaining <= 0f) break;
                }
                yield return null;
            }

            if (roundSolved && suggestUI != null) suggestUI.FlashSolved();

            // 解出后：解订阅、等一拍、刷新被解那侧、再选 active + 新球
            UnsubscribeActive();
            yield return new WaitForSeconds(interRoundDelay);

            // 只刷新刚刚被解的那一侧（"该侧颜色刷新"）
            RandomizeSide(activeIsLeft);
            PushAllUIColors();

            // 重新选 active（可能再次落到刚刚那侧 / 也可能是另一侧）
            activeIsLeft = (Random.value < 0.5f);

            // 即将激活的这侧把平台清回原色，避免上一回合残留导致瞬解
            ResetRegionPlatforms(activeRegion);

            SubscribeActive();
            SpawnBallForActiveSide();
        }
    }

    /// <summary>编辑器右键 → 立即跑一次回合（不进 Play 也能用，但 ColorChangePlatform 的颜色解析需要运行时）。</summary>
    [ContextMenu("Debug / Force Refresh Active Side")]
    public void DebugForceRefreshActive()
    {
        RandomizeSide(activeIsLeft);
        PushAllUIColors();
        ResetRegionPlatforms(activeRegion);
        SpawnBallForActiveSide();
    }

    // ── 工具：随机化一侧的目标色 + 推到平台 ──
    void RandomizeSide(bool isLeft)
    {
        var region = isLeft ? leftRegion : rightRegion;
        if (region == null || region.platforms == null) return;

        var faces = PickFaces(region.platforms.Count, allowDuplicateColors);
        if (isLeft) leftFaces = faces; else rightFaces = faces;

        ResetRegionPlatforms(region);
        ApplyFacesToRegion(region, faces);
        roundNumber++;
    }

    // ── 工具：把当前 leftFaces + rightFaces 全部推到 BossSuggestUI ──
    void PushAllUIColors()
    {
        if (suggestUI == null) return;
        int total = leftFaces.Count + rightFaces.Count;
        Color[] colors = new Color[total];
        for (int i = 0; i < leftFaces.Count; i++)
        {
            Color c = ResolveColor(leftFaces[i]);
            if (dimInactiveSide && !activeIsLeft) c = TintBy(c, inactiveSideTint);
            colors[i] = c;
        }
        for (int i = 0; i < rightFaces.Count; i++)
        {
            Color c = ResolveColor(rightFaces[i]);
            if (dimInactiveSide && activeIsLeft) c = TintBy(c, inactiveSideTint);
            colors[leftFaces.Count + i] = c;
        }
        suggestUI.SetColors(colors);
        suggestUI.SetTimerFill(1f);
    }

    // ── 工具：按当前 active 边生成颜色球 ──
    void SpawnBallForActiveSide()
    {
        if (currentBall != null) Destroy(currentBall);
        var activeFaces = activeIsLeft ? leftFaces : rightFaces;
        int idx = activeRegion.ballPlatformIndex;

        Vector3? spawnPos = ResolveBallSpawnPosition();
        if (ballPrefab != null && spawnPos.HasValue
            && idx >= 0 && idx < activeFaces.Count)
        {
            currentBall = Instantiate(ballPrefab, spawnPos.Value, Quaternion.identity);
            var carrier = currentBall.GetComponent<BallColorCarrier>();
            if (carrier != null) carrier.SetFace(activeFaces[idx]);
            else Debug.LogWarning("[BossArena] ball prefab 缺少 BallColorCarrier，无法控制颜色");
        }
        else if (ballPrefab != null)
        {
            Debug.LogWarning("[BossArena] 无法生成颜色球：ballSpawnPoint / ballSpawnZone 都未配置");
        }

        // 重置看门狗计时
        currentBallSpawnTime = Time.time;
        currentBallStuckTimer = 0f;

        if (verboseLog)
        {
            Debug.Log($"[BossArena] Round {roundNumber}: active={(activeIsLeft ? "LEFT" : "RIGHT")}, " +
                $"left=[{string.Join(",", leftFaces)}], right=[{string.Join(",", rightFaces)}], " +
                $"ball={(activeIsLeft ? leftFaces[leftRegion.ballPlatformIndex] : rightFaces[rightRegion.ballPlatformIndex])}");
        }
    }

    // ── 颜色球看门狗 ──
    /// <summary>
    /// 周期性检查 currentBall：
    ///   1) 已被销毁（玩家把球弄掉到死亡区） → 立即重生；
    ///   2) 速度长时间低于阈值（卡墙缝、卡角落） → 重生；
    ///   3) 当前回合已解 / 还没出球 / 在生成豁免期内 → 跳过。
    /// 不会在非 active 回合（roundSolved 已 true 但还没进入下一回合）期间重生，避免视觉混乱。
    /// </summary>
    IEnumerator BallWatchdog()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.1f, ballWatchdogInterval));
        while (true)
        {
            yield return wait;
            if (!autoRefreshStuckBall) continue;
            if (roundSolved) continue;                      // 本回合已解，等下一回合
            if (currentBallSpawnTime <= 0f) continue;       // 还没出过球

            // 情况 1：球被销毁（被吞、出界、life-time 触发等）
            if (currentBall == null)
            {
                if (verboseLog) Debug.Log("[BossArena] 球丢失，重新生成");
                RespawnActiveBall();
                continue;
            }

            // 球刚出生：豁免期不检测，让它自由落体
            if (Time.time - currentBallSpawnTime < ballSpawnImmunity) continue;

            // 情况 2：速度持续低于阈值
            var rb = currentBall.GetComponent<Rigidbody2D>();
            float speed = rb != null ? rb.velocity.magnitude : 0f;
            if (speed < ballStuckSpeedThreshold)
            {
                currentBallStuckTimer += ballWatchdogInterval;
                if (currentBallStuckTimer >= ballStuckGraceTime)
                {
                    if (verboseLog) Debug.Log($"[BossArena] 球卡住 {ballStuckGraceTime}s，重新生成");
                    RespawnActiveBall();
                }
            }
            else
            {
                currentBallStuckTimer = 0f;                 // 还在动 → 重置计时
            }
        }
    }

    /// <summary>立刻销毁当前球并在 spawn 点重新生成同色新球。</summary>
    void RespawnActiveBall()
    {
        // 直接复用 SpawnBallForActiveSide：它本身会先销毁旧球
        SpawnBallForActiveSide();
    }

    void SubscribeActive()
    {
        if (activeRegion.group != null)
            activeRegion.group.OnAllConditionsMet += HandleActiveGroupMet;
    }

    void UnsubscribeActive()
    {
        if (leftRegion.group != null)  leftRegion.group.OnAllConditionsMet  -= HandleActiveGroupMet;
        if (rightRegion.group != null) rightRegion.group.OnAllConditionsMet -= HandleActiveGroupMet;
    }

    void HandleActiveGroupMet()
    {
        if (roundSolved) return;
        roundSolved = true;
        if (verboseLog) Debug.Log($"[BossArena] 解出 round {roundNumber}（{(activeIsLeft ? "LEFT" : "RIGHT")}），掉落奖励箱");

        // 掉箱：优先用 active 区自己的掉落点，没设就用 fallback
        Transform drop = activeRegion.rewardDropPoint != null
            ? activeRegion.rewardDropPoint
            : fallbackRewardDropPoint;
        if (rewardBoxPrefab != null && drop != null)
            Instantiate(rewardBoxPrefab, drop.position, Quaternion.identity);
        else if (rewardBoxPrefab != null)
            Instantiate(rewardBoxPrefab, transform.position, Quaternion.identity);

        // 顺手清掉残留的球
        if (currentBall != null) Destroy(currentBall);
    }

    // ────────── 工具 ──────────

    /// <summary>颜色球生成位置：优先 RainZone 随机点，然后 ballSpawnPoint，都没就 null。</summary>
    Vector3? ResolveBallSpawnPosition()
    {
        if (ballSpawnZone != null) return ballSpawnZone.GetRandomPosition();
        if (ballSpawnPoint != null) return ballSpawnPoint.position;
        return null;
    }


    void ResetRegionPlatforms(RegionConfig r)
    {
        if (r == null) return;
        foreach (var p in r.platforms) if (p != null) p.ResetPlatformColor();
    }

    void ApplyFacesToRegion(RegionConfig r, List<CubeFace> faces)
    {
        if (r == null) return;
        for (int i = 0; i < r.platforms.Count && i < faces.Count; i++)
            if (r.platforms[i] != null) r.platforms[i].SetRequiredFace(faces[i]);
    }

    List<CubeFace> PickFaces(int count, bool allowDup)
    {
        var list = new List<CubeFace>(count);
        if (allowDup || count > 4)
        {
            for (int i = 0; i < count; i++)
                list.Add((CubeFace)Random.Range(0, 4));
        }
        else
        {
            var pool = new List<CubeFace> { CubeFace.Yellow, CubeFace.Blue, CubeFace.Red, CubeFace.Green };
            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(0, pool.Count);
                list.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
        }
        return list;
    }

    Color ResolveColor(CubeFace face)
    {
        if (colorSensor != null)
        {
            switch (face)
            {
                case CubeFace.Yellow: return colorSensor.faceUp;
                case CubeFace.Blue:   return colorSensor.faceDown;
                case CubeFace.Red:    return colorSensor.faceLeft;
                case CubeFace.Green:  return colorSensor.faceRight;
            }
        }
        switch (face)
        {
            case CubeFace.Yellow: return Color.yellow;
            case CubeFace.Blue:   return Color.blue;
            case CubeFace.Red:    return Color.red;
            case CubeFace.Green:  return Color.green;
        }
        return Color.white;
    }

    static Color TintBy(Color c, Color tint)
    {
        // 简单乘法叠色：(1,1,1,1)=不变；(0.4,0.4,0.4,1)=变暗约 60%
        return new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a * tint.a);
    }
}
