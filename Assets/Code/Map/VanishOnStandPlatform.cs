using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class StableVanishPlatform : MonoBehaviour
{
    [Header("核心设置")]
    [Min(0.1f)] public float standTimeToVanish = 2f; // 站多久后消失
    public float respawnTime = 5f; // 消失后复活时间 (0=永久消失)

    [Header("功能开关")]
    public bool isOneTime = false; // 一次性消失（不复活）
    public bool resetTimerOnLeave = true; // 离开平台就重置计时（推荐开启）

    // 内部状态
    private Collider2D _collider;
    private Renderer _renderer;
    private Rigidbody2D _rb;
    private float _standTimer; // 站立计时
    private bool _isPlayerOnPlatform; // 玩家是否在平台上
    private bool _isVanished; // 是否已消失

    void Awake()
    {
        // 自动获取组件
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<Renderer>();
        _rb = GetComponent<Rigidbody2D>();

        // 【关键】强制正确物理设置，杜绝穿模/检测失效
        _collider.isTrigger = false; // 非触发器 = 可以踩
        _rb.bodyType = RigidbodyType2D.Static; // 静态地块
        _rb.gravityScale = 0;
        _rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // 禁止休眠（稳定检测）

        // 初始化平台为激活状态
        SetPlatformActive(true);
    }

    // 持续检测：玩家站在平台上
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player") && !_isVanished)
        {
            _isPlayerOnPlatform = true;
        }
    }

    // 检测：玩家离开平台
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            _isPlayerOnPlatform = false;

            // 开启则离开就重置计时（最稳定的玩法）
            if (resetTimerOnLeave) _standTimer = 0;
        }
    }

    void Update()
    {
        // 只有玩家站在上面 且 未消失 才计时
        if (_isPlayerOnPlatform && !_isVanished)
        {
            _standTimer += Time.deltaTime;

            // 计时达到，立即消失
            if (_standTimer >= standTimeToVanish)
            {
                Vanish();
            }
        }

        // 颜色提示：白→黄→红
        if (_isPlayerOnPlatform && !_isVanished && _renderer != null)
        {
            float t = _standTimer / standTimeToVanish;
            _renderer.material.color = Color.Lerp(Color.white, Color.red, t);
        }
    }

    // 平台消失逻辑
    void Vanish()
    {
        _isVanished = true;
        SetPlatformActive(false);
        _standTimer = 0;

        // 非一次性平台，启动复活
        if (!isOneTime && respawnTime > 0)
        {
            Invoke(nameof(Respawn), respawnTime);
        }
    }

    // 平台复活逻辑
    void Respawn()
    {
        if (_renderer != null) _renderer.material.color = Color.white;

        SetPlatformActive(true);
        _isVanished = false;
        _isPlayerOnPlatform = false;
    }

    // 统一控制平台显隐/碰撞
    void SetPlatformActive(bool active)
    {
        _collider.enabled = active;
        if (_renderer != null) _renderer.enabled = active;
    }

    // 关卡重置时调用（可选）
    public void ResetPlatform()
    {
        CancelInvoke();
        SetPlatformActive(true);
        _standTimer = 0;
        _isVanished = false;
        _isPlayerOnPlatform = false;
    }
}