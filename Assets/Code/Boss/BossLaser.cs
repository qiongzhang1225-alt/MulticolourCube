using UnityEngine;
using System.Collections;

/// <summary>
/// Boss 激光：
///   1. 蓄力阶段（chargeTime）：显示细红色警示线，**持续追踪玩家**——给玩家逃跑信号
///   2. 激活阶段（activeTime）：在蓄力结束的瞬间锁定方向，**不再追踪**——玩家有机会跑出射线
/// 长度由 BossTriangle 在 Activate 前设置 laserLength（默认会拉到整个场地对角线）。
/// 使用 LineRenderer 渲染，BoxCollider2D 作为伤害检测区。
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BossLaser : MonoBehaviour
{
    [Header("外观")]
    public float chargeWidth = 0.08f;
    public float activeWidth = 0.6f;
    public Color chargeColor = new Color(1f, 0.4f, 0.4f, 0.6f);
    public Color activeColor = new Color(1f, 0.2f, 0.5f, 1f);

    [Header("范围")]
    [Tooltip("激光长度（外部可在生成后覆盖）")]
    public float laserLength = 50f;

    [Header("追踪")]
    [Tooltip("蓄力期追踪角速度（度/秒）。值越小激光越笨拙，玩家越容易甩脱。-1 表示瞬间锁定（无延迟，难度极高）。")]
    public float trackingDegreesPerSecond = 90f;
    [Tooltip("初始瞄准偏差（度，正负随机）——避免站着不动也被精准命中")]
    public float initialAimError = 25f;

    private LineRenderer line;
    private BoxCollider2D hitBox;
    private bool isActive = false;

    // 当前激光朝向（度）
    private float currentAngle;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.startWidth = chargeWidth;
        line.endWidth = chargeWidth;
        line.startColor = chargeColor;
        line.endColor = chargeColor;

        // BoxCollider2D 作为伤害检测区
        hitBox = gameObject.AddComponent<BoxCollider2D>();
        hitBox.isTrigger = true;
        hitBox.enabled = false;
    }

    /// <summary>
    /// 激活激光。chargeTime 内显示细警示线（持续追踪），之后 activeTime 内变粗激活伤害（继续追踪）。
    /// </summary>
    public void Activate(Transform source, Transform target, float chargeTime, float activeTime)
    {
        StartCoroutine(LaserSequence(source, target, chargeTime, activeTime));
    }

    IEnumerator LaserSequence(Transform source, Transform target, float chargeTime, float activeTime)
    {
        // 初始角度对齐到玩家 + 随机偏差（避免一开始就精准命中）
        float baseAngle = ComputeAngleToTarget(source, target);
        float aimErr = (initialAimError > 0f) ? Random.Range(-initialAimError, initialAimError) : 0f;
        currentAngle = baseAngle + aimErr;

        // ── 蓄力阶段（追踪 + 细线警示）──
        // 注意：不再调用 UpdateLaserAim(instant:true)，否则会覆盖 initialAimError 的偏差
        ApplyVisual(chargeColor, chargeWidth);
        UpdateLaserPositionOnly(source); // 用刚算出的带偏差 currentAngle 显示警示线

        float t = 0f;
        while (t < chargeTime)
        {
            UpdateLaserAim(source, target, instant: false);
            t += Time.deltaTime;
            yield return null;
        }

        // ── 蓄力结束：直接用当前 currentAngle 锁定（这是被 trackingDegreesPerSecond 限速追踪到的角度）──
        // 不再调用 UpdateLaserAim —— 否则会瞬间对齐玩家当前位置，等于白蓄力
        UpdateLaserPositionOnly(source);

        // ── 激活阶段（方向已锁定，只跟随 source 位置移动）──
        ApplyVisual(activeColor, activeWidth);
        hitBox.size = new Vector2(laserLength, activeWidth);
        hitBox.offset = new Vector2(laserLength * 0.5f, 0f);
        hitBox.enabled = true;
        isActive = true;

        t = 0f;
        while (t < activeTime)
        {
            // 不再传 target —— 角度保持锁定，仅同步 source 位置
            UpdateLaserPositionOnly(source);
            t += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>激活阶段使用：方向已锁定，只更新位置跟随 source。</summary>
    void UpdateLaserPositionOnly(Transform source)
    {
        if (source == null) return;
        transform.position = source.position;
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, new Vector3(laserLength, 0f, 0f));
    }

    /// <summary>每帧更新激光位置 + 朝向，跟随玩家。instant=true 时直接对齐（无插值）。</summary>
    void UpdateLaserAim(Transform source, Transform target, bool instant)
    {
        if (source == null) return;
        transform.position = source.position;

        if (target != null)
        {
            float targetAngle = ComputeAngleToTarget(source, target);
            if (instant || trackingDegreesPerSecond < 0f)
            {
                currentAngle = targetAngle;
            }
            else
            {
                currentAngle = Mathf.MoveTowardsAngle(
                    currentAngle, targetAngle, trackingDegreesPerSecond * Time.deltaTime);
            }
        }
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

        // 重画 LineRenderer（沿本地 +X 方向）
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, new Vector3(laserLength, 0f, 0f));

        // 同步 hitBox（朝向已由 transform 旋转处理，只需调整大小/偏移）
        if (hitBox.enabled)
        {
            hitBox.size = new Vector2(laserLength, activeWidth);
            hitBox.offset = new Vector2(laserLength * 0.5f, 0f);
        }
    }

    float ComputeAngleToTarget(Transform source, Transform target)
    {
        if (source == null || target == null) return currentAngle;
        Vector2 dir = ((Vector2)target.position - (Vector2)source.position);
        if (dir.sqrMagnitude < 0.0001f) return currentAngle;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    void ApplyVisual(Color c, float w)
    {
        line.startColor = c;
        line.endColor   = c;
        line.startWidth = w;
        line.endWidth   = w;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive) return;
        if (other.CompareTag("Player"))
        {
            var respawn = other.GetComponent<PlayerRespawn>();
            if (respawn != null && !respawn.IsInvincible)
                respawn.Die();
        }
    }
}
