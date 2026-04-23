using UnityEngine;

/// <summary>
/// 站立消失平台：玩家站立一段时间后平台消失，可选自动复活。
/// 实现 IResettable，死亡/检查点时自动恢复。
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class StableVanishPlatform : MonoBehaviour, IResettable
{
    [Header("消失参数")]
    [Min(0.1f)] public float standTimeToVanish = 2f;
    public float respawnTime = 5f;

    [Header("功能控制")]
    public bool isOneTime = false;
    public bool resetTimerOnLeave = true;

    // 内部状态
    private Collider2D _collider;
    private Renderer _renderer;
    private Rigidbody2D _rb;
    private float _standTimer;
    private bool _isPlayerOnPlatform;
    private bool _isVanished;

    // 检查点存档
    private bool _savedIsVanished;
    private bool _savedColliderEnabled;
    private bool _savedRendererEnabled;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<Renderer>();
        _rb = GetComponent<Rigidbody2D>();

        _collider.isTrigger = false;
        _rb.bodyType = RigidbodyType2D.Static;
        _rb.gravityScale = 0;
        _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        SetPlatformActive(true);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player") && !_isVanished)
            _isPlayerOnPlatform = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            _isPlayerOnPlatform = false;
            if (resetTimerOnLeave) _standTimer = 0;
        }
    }

    void Update()
    {
        if (_isPlayerOnPlatform && !_isVanished)
        {
            _standTimer += Time.deltaTime;
            if (_standTimer >= standTimeToVanish)
                Vanish();
        }

        // 颜色渐变提示
        if (_isPlayerOnPlatform && !_isVanished && _renderer != null)
        {
            float t = _standTimer / standTimeToVanish;
            _renderer.material.color = Color.Lerp(Color.white, Color.red, t);
        }
    }

    void Vanish()
    {
        _isVanished = true;
        SetPlatformActive(false);
        _standTimer = 0;

        if (!isOneTime && respawnTime > 0)
            Invoke(nameof(Respawn), respawnTime);
    }

    void Respawn()
    {
        if (_renderer != null) _renderer.material.color = Color.white;
        SetPlatformActive(true);
        _isVanished = false;
        _isPlayerOnPlatform = false;
    }

    void SetPlatformActive(bool active)
    {
        _collider.enabled = active;
        if (_renderer != null) _renderer.enabled = active;
    }

    // ── 重置（手动调用）──
    public void ResetPlatform()
    {
        CancelInvoke();
        SetPlatformActive(true);
        _standTimer = 0;
        _isVanished = false;
        _isPlayerOnPlatform = false;
        if (_renderer != null) _renderer.material.color = Color.white;
    }

    // ── IResettable ──

    public void SaveCheckpointState()
    {
        _savedIsVanished = _isVanished;
        _savedColliderEnabled = _collider.enabled;
        _savedRendererEnabled = _renderer != null && _renderer.enabled;
    }

    public void ResetToCheckpointState()
    {
        CancelInvoke();
        _standTimer = 0;
        _isPlayerOnPlatform = false;
        _isVanished = _savedIsVanished;
        _collider.enabled = _savedColliderEnabled;
        if (_renderer != null)
        {
            _renderer.enabled = _savedRendererEnabled;
            _renderer.material.color = Color.white;
        }
    }
}
