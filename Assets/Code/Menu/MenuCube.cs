using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 首页展示方块：点击翻滚，根据朝下的面通知背景切换颜色。
/// 轻量设计，不依赖 BlockController / PlayerColorSensor 等游戏组件。
/// </summary>
public class MenuCube : MonoBehaviour
{
    [Header("翻滚参数")]
    public float rollDuration = 0.25f;
    public float rollCooldown = 0.3f;

    [Header("四面颜色（方块自身显示色）")]
    public Color bottomColor = new Color(1f, 0.922f, 0.016f);   // 黄
    public Color topColor    = new Color(0f, 0.898f, 0.937f);    // 蓝
    public Color leftColor   = Color.red;                         // 红
    public Color rightColor  = Color.green;                       // 绿

    /// <summary>当朝下的面发生变化时触发（参数：面索引 0-3）。</summary>
    public event Action<int> OnFaceChanged;

    /// <summary>当前朝下的面索引（0=Bottom, 1=Left, 2=Top, 3=Right）。</summary>
    public int CurrentFaceIndex { get; private set; } = 0;

    // 内部
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool isRolling = false;
    private float lastRollTime = -999f;

    // 面颜色数组（按面索引排列）
    private Color[] faceColors;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        RebuildColorArray();

        // 确保 Rigidbody2D 不会因重力移动方块
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        // 方块渲染层级高于泡泡，防止颜色叠加
        if (sr != null)
            sr.sortingOrder = 10;
    }

    void Start()
    {
        UpdateFaceVisual();
    }

    void Update()
    {
        if (isRolling) return;
        if (Time.time - lastRollTime < rollCooldown) return;

        // 鼠标点击翻滚
        if (Input.GetMouseButtonDown(0))
        {
            // 忽略 UI 点击
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int direction = mouseWorld.x > transform.position.x ? 1 : -1;
            StartCoroutine(Roll(direction));
        }
    }

    // ──────── 旋转动画（以中心为轴，原地旋转 90°）────────

    IEnumerator Roll(int direction)
    {
        isRolling = true;
        lastRollTime = Time.time;

        float rotated = 0f;
        float totalAngle = 90f;

        while (rotated < totalAngle)
        {
            float step = (totalAngle / rollDuration) * Time.deltaTime;
            step = Mathf.Min(step, totalAngle - rotated);
            rotated += step;
            transform.Rotate(0f, 0f, step * direction);
            yield return null;
        }

        // 对齐到 90°
        float z = Mathf.Round(transform.eulerAngles.z / 90f) * 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, z);

        // 更新面
        int newFace = GetDownFaceIndex();
        if (newFace != CurrentFaceIndex)
        {
            CurrentFaceIndex = newFace;
            UpdateFaceVisual();
            OnFaceChanged?.Invoke(CurrentFaceIndex);
        }

        isRolling = false;
    }

    // ──────── 面检测（纯旋转计算，无需物理）────────

    /// <summary>
    /// 根据当前 Z 旋转判断哪个面朝下。
    /// 0°→Bottom, 90°→Left, 180°→Top, 270°→Right
    /// </summary>
    private int GetDownFaceIndex()
    {
        float angle = transform.eulerAngles.z % 360f;
        if (angle < 0) angle += 360f;
        int index = Mathf.RoundToInt(angle / 90f) % 4;
        return index;
    }

    // ──────── 显示 ────────

    private void UpdateFaceVisual()
    {
        if (sr != null && faceColors != null && CurrentFaceIndex < faceColors.Length)
        {
            sr.color = faceColors[CurrentFaceIndex];
        }
    }

    private void RebuildColorArray()
    {
        faceColors = new Color[] { bottomColor, leftColor, topColor, rightColor };
    }

    /// <summary>获取指定面索引对应的颜色。</summary>
    public Color GetFaceColor(int faceIndex)
    {
        if (faceColors == null) RebuildColorArray();
        return faceColors[Mathf.Clamp(faceIndex, 0, 3)];
    }
}
