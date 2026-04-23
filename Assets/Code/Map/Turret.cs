using UnityEngine;

/// <summary>
/// 炮台：按固定间隔发射子弹。
/// 可选颜色弹幕模式——按指定规律循环发射不同颜色的子弹，
/// 用于给玩家提供颜色方块谜题的提示。
/// </summary>
public class Turret : MonoBehaviour, IResettable
{
    [Header("基础设置")]
    public GameObject bulletPrefab;   // 子弹预制体
    public Transform firePoint;       // 炮口位置
    public float fireRate = 1f;       // 发射间隔（秒）
    public float bulletSpeed = 10f;   // 子弹速度

    [Header("颜色弹幕（可选）")]
    [Tooltip("启用后，子弹按 colorPattern 数组循环着色")]
    public bool useColorPattern = false;

    [Tooltip("颜色循环序列，如 黄→绿→蓝→黄→…")]
    public Color[] colorPattern = new Color[]
    {
        Color.yellow,
        Color.green,
        Color.blue
    };

    public enum ColorOrder
    {
        Sequential,   // 顺序循环：0→1→2→0→…
        Random,       // 随机选取
    }

    [Tooltip("颜色选取方式")]
    public ColorOrder colorOrder = ColorOrder.Sequential;

    // ── 内部状态 ──
    private float fireTimer;
    private int colorIndex = 0;

    private void Update()
    {
        fireTimer += Time.deltaTime;

        if (fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("[Turret] 缺少 prefab 或 firePoint！");
            return;
        }

        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // 设置子弹速度
        Rigidbody2D rb = bulletObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = firePoint.right * bulletSpeed;
        }

        // ── 颜色着色 ──
        if (useColorPattern && colorPattern != null && colorPattern.Length > 0)
        {
            Color c = PickNextColor();
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.SetBulletColor(c);
            }
        }
    }

    /// <summary>按规律选取下一个颜色。</summary>
    private Color PickNextColor()
    {
        Color c;
        switch (colorOrder)
        {
            case ColorOrder.Random:
                c = colorPattern[Random.Range(0, colorPattern.Length)];
                break;

            case ColorOrder.Sequential:
            default:
                c = colorPattern[colorIndex];
                colorIndex = (colorIndex + 1) % colorPattern.Length;
                break;
        }
        return c;
    }

    /// <summary>重置颜色序列到起点（关卡重置时可调用）。</summary>
    public void ResetColorSequence()
    {
        colorIndex = 0;
        fireTimer = 0f;
    }

    // ── IResettable ──
    private int _savedColorIndex;
    private float _savedFireTimer;

    public void SaveCheckpointState()
    {
        _savedColorIndex = colorIndex;
        _savedFireTimer = fireTimer;
    }

    public void ResetToCheckpointState()
    {
        colorIndex = _savedColorIndex;
        fireTimer = _savedFireTimer;
    }
}
