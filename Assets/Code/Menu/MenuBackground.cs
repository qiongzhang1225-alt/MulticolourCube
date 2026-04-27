using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 首页背景控制器：监听 MenuCube 的面变化，平滑过渡背景颜色。
/// 同时控制相机背景色和 UI 面板颜色。
/// </summary>
public class MenuBackground : MonoBehaviour
{
    [Header("关联方块")]
    public MenuCube menuCube;

    [Header("自定义背景颜色（与方块四面对应）")]
    [Tooltip("底面朝下时的背景色")]
    public Color bgBottom = new Color(0.95f, 0.85f, 0.30f);   // 暖黄
    [Tooltip("左面朝下时的背景色")]
    public Color bgLeft   = new Color(0.85f, 0.25f, 0.25f);   // 深红
    [Tooltip("顶面朝下时的背景色")]
    public Color bgTop    = new Color(0.20f, 0.60f, 0.85f);   // 天蓝
    [Tooltip("右面朝下时的背景色")]
    public Color bgRight  = new Color(0.25f, 0.75f, 0.35f);   // 草绿

    [Header("过渡")]
    public float transitionSpeed = 3f;

    [Header("可选 UI 面板（同步变色）")]
    public Image backgroundPanel;

    // 内部
    private Camera mainCam;
    private Color targetColor;
    private Color[] bgColors;

    void Awake()
    {
        mainCam = Camera.main;
        bgColors = new Color[] { bgBottom, bgLeft, bgTop, bgRight };

        // 初始颜色
        targetColor = bgColors[0];
        if (mainCam != null)
            mainCam.backgroundColor = targetColor;
        if (backgroundPanel != null)
            backgroundPanel.color = targetColor;
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
        // 平滑过渡
        if (mainCam != null)
            mainCam.backgroundColor = Color.Lerp(mainCam.backgroundColor, targetColor, Time.deltaTime * transitionSpeed);

        if (backgroundPanel != null)
            backgroundPanel.color = Color.Lerp(backgroundPanel.color, targetColor, Time.deltaTime * transitionSpeed);
    }

    private void OnFaceChanged(int faceIndex)
    {
        if (faceIndex >= 0 && faceIndex < bgColors.Length)
        {
            targetColor = bgColors[faceIndex];
        }
    }
}
