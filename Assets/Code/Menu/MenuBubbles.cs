using UnityEngine;

/// <summary>
/// 首页泡泡特效：从画面底部缓缓升起半透明泡泡，
/// 颜色随 MenuCube 面变化而过渡，诱导玩家点击探索。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class MenuBubbles : MonoBehaviour
{
    [Header("关联")]
    public MenuCube menuCube;

    [Header("泡泡颜色（与方块四面对应）")]
    public Color bubbleBottom = new Color(1f, 0.95f, 0.5f, 0.35f);   // 淡黄
    public Color bubbleLeft   = new Color(1f, 0.5f, 0.5f, 0.35f);    // 淡红
    public Color bubbleTop    = new Color(0.5f, 0.8f, 1f, 0.35f);    // 淡蓝
    public Color bubbleRight  = new Color(0.5f, 1f, 0.6f, 0.35f);    // 淡绿

    [Header("过渡速度")]
    public float colorTransitionSpeed = 2f;

    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private Color targetColor;
    private Color currentColor;
    private Color[] bubbleColors;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        mainModule = ps.main;
        bubbleColors = new Color[] { bubbleBottom, bubbleLeft, bubbleTop, bubbleRight };

        currentColor = bubbleColors[0];
        targetColor = currentColor;
    }

    void OnEnable()
    {
        if (menuCube != null)
            menuCube.OnFaceChanged += OnFaceChanged;
    }

    void OnDisable()
    {
        if (menuCube != null)
            menuCube.OnFaceChanged -= OnFaceChanged;
    }

    void Update()
    {
        // 平滑过渡泡泡颜色
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);
        var startColor = mainModule.startColor;

        // 使用两色渐变，让泡泡有轻微色差更自然
        Color lighter = currentColor;
        lighter.a = currentColor.a * 0.6f;

        Color brighter = currentColor;
        brighter.r = Mathf.Min(1f, brighter.r + 0.15f);
        brighter.g = Mathf.Min(1f, brighter.g + 0.15f);
        brighter.b = Mathf.Min(1f, brighter.b + 0.15f);
        brighter.a = currentColor.a;

        mainModule.startColor = new ParticleSystem.MinMaxGradient(lighter, brighter);
    }

    private void OnFaceChanged(int faceIndex)
    {
        if (faceIndex >= 0 && faceIndex < bubbleColors.Length)
        {
            targetColor = bubbleColors[faceIndex];
        }
    }
}
