using UnityEngine;
using System;

// 按钮触发事件脚本（挂载到按钮对象）
public class ButtonTrigger : MonoBehaviour
{
    // 按钮触发时的回调事件（供移动平台订阅）
    public event Action OnButtonTriggered;

    [Header("按钮设置")]
    public string triggerTag = "Player";       // 触发按钮的标签
    public bool isOneTime = true;              // 是否仅触发一次

    private Collider2D col;
    private bool isTriggered = false;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true; // 按钮必须是Trigger
        gameObject.tag = "Button"; // 强制设置标签（避免漏设）
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag) && !isTriggered)
        {
            isTriggered = true;
            OnButtonTriggered?.Invoke(); // 触发事件

            // 一次性按钮：禁用自身
            if (isOneTime)
            {
                col.enabled = false;
                Renderer rend = GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
            }
        }
    }

    // 重置按钮（可选：关卡重置时调用）
    public void ResetButton()
    {
        isTriggered = false;
        col.enabled = true;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = true;
    }
}