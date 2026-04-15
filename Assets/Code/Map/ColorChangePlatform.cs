using UnityEngine;

// 可变色地块：适配自由旋转的玩家方块
public class ColorChangePlatform : MonoBehaviour
{
    [Header("颜色设置")]
    public bool revertColorOnLeave = true; // 玩家离开后恢复原色
    private Color _originalColor;
    private Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;
    }

    // 碰撞触发变色（适配旋转方块）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        PlayerColorSensor sensor = collision.collider.GetComponent<PlayerColorSensor>();
        if (sensor == null) return;

        // 获取当前接触面的颜色（自动适配旋转）
        Color targetColor = sensor.GetContactFaceColor(collision);
        ChangeColor(targetColor);
    }

    // 离开恢复原色
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player") && revertColorOnLeave)
            ChangeColor(_originalColor);
    }

    void ChangeColor(Color color)
    {
        if (_renderer != null) _renderer.material.color = color;
    }

    // 关卡重置调用
    public void ResetPlatformColor() => ChangeColor(_originalColor);
}