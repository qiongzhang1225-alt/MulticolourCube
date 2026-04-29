using UnityEngine;

/// <summary>
/// 统一输入适配层：同时支持「键盘+鼠标」和「手柄」。
///
/// 设计原则：
///   - 玩家输入只通过这里读取，避免到处散落 Input.GetKey/GetMouseButton；
///   - Horizontal / Vertical 直接复用 Unity InputManager 的同名轴，
///     该轴在 InputManager.asset 中既绑了 WASD/方向键，也绑了手柄左摇杆；
///   - 其它按钮自定义额外的轴名（_Pad 后缀）映射到手柄按键。
///
/// 默认手柄映射（Xbox 命名；PS 手柄按钮编号一致，仅图标不同）：
///   左摇杆           → Horizontal / Vertical
///   A (joy btn 0)    → Jump_Pad
///   B (joy btn 1)    → Teleport_Pad
///   X (joy btn 2)    → Roll_Pad
///   Start (btn 7)    → Pause_Pad
///
/// 键盘鼠标映射（保持原有交互不变）：
///   WASD/方向键      → 移动 / 攀爬
///   Space            → 跳
///   鼠标左键         → 翻滚（方向取自鼠标 X 与玩家相对位置）
///   鼠标右键         → 传送锚点
///   Esc              → 暂停 / 设置
/// </summary>
public static class InputAdapter
{
    /// <summary>左摇杆死区（小于此值视为零，用来过滤手柄漂移）。</summary>
    public const float STICK_DEADZONE = 0.25f;

    // ────── 移动 ──────
    public static float Horizontal => Input.GetAxisRaw("Horizontal");
    public static float Vertical   => Input.GetAxisRaw("Vertical");

    // ────── 跳跃 ──────
    public static bool JumpPressed =>
        Input.GetKeyDown(KeyCode.Space) || SafeButtonDown("Jump_Pad");

    // ────── 暂停 / 打开设置面板 ──────
    public static bool PausePressed =>
        Input.GetKeyDown(KeyCode.Escape) || SafeButtonDown("Pause_Pad");

    // ────── 翻滚 ──────
    /// <summary>翻滚被按下（鼠标左键 或 手柄 X 键）。</summary>
    public static bool RollPressed =>
        Input.GetMouseButtonDown(0) || SafeButtonDown("Roll_Pad");

    /// <summary>本帧的翻滚是否来自鼠标（决定方向取鼠标位置还是摇杆）。</summary>
    public static bool RollPressedByMouse => Input.GetMouseButtonDown(0);

    // ────── 传送锚点 ──────
    public static bool TeleportPressed =>
        Input.GetMouseButtonDown(1) || SafeButtonDown("Teleport_Pad");

    /// <summary>
    /// 计算翻滚方向（-1 = 向左，+1 = 向右）。
    ///   - 鼠标触发：比较鼠标世界 X 与角色 X；
    ///   - 手柄触发：取左摇杆 X 的符号；摇杆未推时默认 +1（向右）。
    /// </summary>
    public static int GetRollDirection(Vector3 selfWorldPos, Camera worldCam)
    {
        if (RollPressedByMouse && worldCam != null)
        {
            Vector3 mp = worldCam.ScreenToWorldPoint(Input.mousePosition);
            return mp.x > selfWorldPos.x ? 1 : -1;
        }
        float h = Horizontal;
        if (h >  STICK_DEADZONE) return  1;
        if (h < -STICK_DEADZONE) return -1;
        return 1;
    }

    // ────── 内部：避免未在 InputManager 中定义按钮时抛异常 ──────
    static bool SafeButtonDown(string name)
    {
        try { return Input.GetButtonDown(name); }
        catch { return false; }
    }
}
