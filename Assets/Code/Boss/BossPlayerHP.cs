using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Boss 关卡专用：玩家生命值（默认 5 点）。
/// 监听 PlayerRespawn.OnPlayerDeath，每次死亡 -1；归零时重新加载本场景。
/// 通过 Heart 拾取调用 Heal(int) 回血。
///
/// 用法：把本组件挂在玩家或一个常驻 Manager 上，并在 Inspector 引用 PlayerRespawn。
/// 若不指定 playerRespawn，会在 Start 自动 Find。
/// </summary>
public class BossPlayerHP : MonoBehaviour
{
    public static BossPlayerHP Instance { get; private set; }

    [Header("血量配置")]
    [Tooltip("初始最大血量")] public int maxHP = 5;
    [Tooltip("当前血量（运行时只读，Inspector 仅用于调试）")]
    [SerializeField] private int currentHP = 5;

    [Header("引用（可留空，自动查找）")]
    public PlayerRespawn playerRespawn;

    [Header("行为")]
    [Tooltip("HP 归零时是否重新加载当前场景")] public bool reloadSceneOnDeath = true;
    [Tooltip("HP 归零到重新加载之间的延迟（秒，配合死亡动画）")]
    public float reloadDelay = 1.2f;

    /// <summary>HP 改变时触发（参数：当前 HP, 最大 HP）。UI 订阅。</summary>
    public event Action<int, int> OnHPChanged;
    /// <summary>HP 归零（玩家彻底失败）时触发。</summary>
    public event Action OnGameOver;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    /// <summary>外部询问：这次受击是否会致命（HP 将归零）。
    /// PlayerRespawn 用它决定是否弹死亡画面：返回 true → 走完整死亡流程；false → 软复活。</summary>
    public bool IsNextHitFatal() => currentHP <= 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        currentHP = maxHP;
    }

    void Start()
    {
        if (playerRespawn == null)
            playerRespawn = FindObjectOfType<PlayerRespawn>();

        if (playerRespawn != null)
            playerRespawn.OnPlayerDeath += HandlePlayerDeath;
        else
            Debug.LogWarning("[BossPlayerHP] 未找到 PlayerRespawn，无法监听死亡事件。");

        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (playerRespawn != null)
            playerRespawn.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath(Vector3 _)
    {
        currentHP = Mathf.Max(0, currentHP - 1);
        OnHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
        {
            OnGameOver?.Invoke();
            if (reloadSceneOnDeath)
                Invoke(nameof(ReloadScene), reloadDelay);
        }
    }

    /// <summary>外部调用：恢复 1 点（或多点）生命。</summary>
    public void Heal(int amount = 1)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void ReloadScene()
    {
        Scene s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }
}
