using UnityEngine;

// 依赖基础移动脚本+按钮触发脚本
[RequireComponent(typeof(MovablePlatform))]
public class ButtonTriggeredMovablePlatform : MonoBehaviour
{
    [Header("按钮触发设置")]
    public GameObject triggerButton;           // 关联的按钮对象
    public float triggerDelay = 0.2f;          // 按下按钮后延迟移动时间

    private MovablePlatform movablePlatform;
    private ButtonTrigger buttonTrigger;
    private bool isMoved = false;

    private void Awake()
    {
        // 初始化基础移动脚本（默认关闭）
        movablePlatform = GetComponent<MovablePlatform>();
        movablePlatform.enabled = false;

        // 绑定按钮事件
        if (triggerButton != null)
        {
            buttonTrigger = triggerButton.GetComponent<ButtonTrigger>();
            if (buttonTrigger == null)
            {
                // 自动给按钮添加触发脚本
                buttonTrigger = triggerButton.AddComponent<ButtonTrigger>();
                Debug.LogWarning("[ButtonPlatform] 按钮缺少ButtonTrigger脚本，已自动添加");
            }
            // 订阅按钮触发事件
            buttonTrigger.OnButtonTriggered += OnButtonPressed;
        }
        else
        {
            Debug.LogError("[ButtonPlatform] 未赋值triggerButton！");
        }
    }

    /// <summary>
    /// 按钮按下后的逻辑
    /// </summary>
    private void OnButtonPressed()
    {
        if (!isMoved)
        {
            Invoke(nameof(StartPlatformMove), triggerDelay);
            isMoved = true;
        }
    }

    /// <summary>
    /// 启动地块移动
    /// </summary>
    private void StartPlatformMove()
    {
        if (movablePlatform != null)
        {
            movablePlatform.enabled = true;
            Debug.Log("[ButtonPlatform] 启动移动：" + gameObject.name);
        }
        else
        {
            Debug.LogError("[ButtonPlatform] 缺少MovablePlatform脚本！");
        }
    }

    // 防止内存泄漏：取消事件订阅
    private void OnDestroy()
    {
        if (buttonTrigger != null)
        {
            buttonTrigger.OnButtonTriggered -= OnButtonPressed;
        }
    }

    // 重置移动状态（可选）
    public void ResetPlatform()
    {
        isMoved = false;
        movablePlatform.enabled = false;
        transform.position = movablePlatform.transform.position; // 重置位置
        if (buttonTrigger != null) buttonTrigger.ResetButton();
    }
}