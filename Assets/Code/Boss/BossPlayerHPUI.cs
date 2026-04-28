using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss 关卡 — 玩家血量 HUD（5 颗心可视化）。
///
/// 订阅 <see cref="BossPlayerHP"/>.OnHPChanged 自动刷新。
/// 支持两种摆放方式（任选其一，也可两者并用）：
///   1) UI 模式：拖入 Image[] hearts，每个 Image 在 Canvas 下；
///   2) 世界空间模式：拖入 SpriteRenderer[] heartSprites，挂在场景物体上。
///
/// 显示风格：
///   - 若设置了 fullSprite/emptySprite，则按"满/空"图替换；
///   - 否则用 fullColor/emptyColor 切换颜色（默认满=红，空=半透明深红）。
///
/// 用法：
///   1) 在场景里建一个 Canvas → 加 5 个 Image（爱心图），把它们拖进 hearts 数组；
///   2) 把本组件挂在任意 GameObject 上；
///   3) 场景里至少存在一个 BossPlayerHP（HP 系统单例）即可。
/// </summary>
public class BossPlayerHPUI : MonoBehaviour
{
    [Header("UI 模式（Canvas 下的 Image）")]
    [Tooltip("按从左到右顺序拖入 5 个心 Image；不用 UI 模式可留空")]
    public Image[] hearts;

    [Header("世界空间模式（场景里的 SpriteRenderer）")]
    [Tooltip("世界空间心形的 SpriteRenderer 数组；不用世界模式可留空")]
    public SpriteRenderer[] heartSprites;

    [Header("贴图（可选；不填则改用颜色切换）")]
    public Sprite fullSprite;
    public Sprite emptySprite;

    [Header("颜色（无贴图时使用）")]
    public Color fullColor = new Color(1f, 0.25f, 0.35f, 1f);
    public Color emptyColor = new Color(0.25f, 0.05f, 0.08f, 0.6f);

    [Header("行为")]
    [Tooltip("HP 改变时让心图轻微缩放一下，作为反馈")]
    public bool pulseOnChange = true;
    [Tooltip("缩放峰值（1 = 不缩放）")] public float pulseScale = 1.25f;
    [Tooltip("缩放持续时间")] public float pulseDuration = 0.18f;

    private BossPlayerHP _hp;
    private int _lastHP = -1;

    void OnEnable()
    {
        TrySubscribe();
    }

    void Start()
    {
        TrySubscribe();
        if (_hp != null) Refresh(_hp.CurrentHP, _hp.MaxHP);
    }

    void OnDisable()
    {
        if (_hp != null) _hp.OnHPChanged -= Refresh;
    }

    void TrySubscribe()
    {
        if (_hp != null) return;
        _hp = BossPlayerHP.Instance != null ? BossPlayerHP.Instance : FindObjectOfType<BossPlayerHP>();
        if (_hp != null)
        {
            _hp.OnHPChanged -= Refresh;
            _hp.OnHPChanged += Refresh;
            Refresh(_hp.CurrentHP, _hp.MaxHP);
        }
    }

    void Update()
    {
        // 兜底：若 HP 单例延迟创建，本组件先于 HP 启用时也能补订阅
        if (_hp == null) TrySubscribe();
    }

    /// <summary>核心刷新：把 [0..currentHP) 的格子点亮，[currentHP..) 的格子置空。</summary>
    public void Refresh(int currentHP, int maxHP)
    {
        ApplyState(hearts, currentHP);
        ApplyState(heartSprites, currentHP);

        if (pulseOnChange && _lastHP >= 0 && _lastHP != currentHP)
        {
            int idx = (currentHP < _lastHP) ? currentHP : currentHP - 1;
            PulseAt(idx);
        }
        _lastHP = currentHP;
    }

    void ApplyState(Image[] arr, int hp)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) continue;
            bool full = i < hp;
            if (fullSprite != null && emptySprite != null)
                arr[i].sprite = full ? fullSprite : emptySprite;
            arr[i].color = full ? fullColor : emptyColor;
            arr[i].enabled = true;
        }
    }

    void ApplyState(SpriteRenderer[] arr, int hp)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) continue;
            bool full = i < hp;
            if (fullSprite != null && emptySprite != null)
                arr[i].sprite = full ? fullSprite : emptySprite;
            arr[i].color = full ? fullColor : emptyColor;
        }
    }

    void PulseAt(int index)
    {
        if (hearts != null && index >= 0 && index < hearts.Length && hearts[index] != null)
            StartCoroutine(PulseCo(hearts[index].rectTransform));
        if (heartSprites != null && index >= 0 && index < heartSprites.Length && heartSprites[index] != null)
            StartCoroutine(PulseCo(heartSprites[index].transform));
    }

    System.Collections.IEnumerator PulseCo(Transform t)
    {
        if (t == null) yield break;
        Vector3 baseScale = t.localScale;
        Vector3 peak = baseScale * pulseScale;
        float half = pulseDuration * 0.5f;
        float k = 0f;
        while (k < half)
        {
            k += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(baseScale, peak, k / half);
            yield return null;
        }
        k = 0f;
        while (k < half)
        {
            k += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(peak, baseScale, k / half);
            yield return null;
        }
        t.localScale = baseScale;
    }

    System.Collections.IEnumerator PulseCo(RectTransform rt)
    {
        yield return PulseCo((Transform)rt);
    }
}
