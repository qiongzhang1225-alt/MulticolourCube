using UnityEngine;
using System;

/// <summary>
/// 按钮触发事件脚本，挂载到按钮物体上。
/// 支持一次性触发和持续触发两种模式。
/// </summary>
public class ButtonTrigger : MonoBehaviour, IResettable
{
    /// <summary>按钮被踩下时触发。</summary>
    public event Action OnButtonTriggered;

    /// <summary>按钮被释放时触发（仅非一次性模式生效）。</summary>
    public event Action OnButtonReleased;

    [Header("按钮设置")]
    public string triggerTag = "Player";       // 能触发按钮的标签
    public bool isOneTime = true;              // true=一次性按钮；false=持续按住型按钮

    private Collider2D col;
    private bool isTriggered = false;

    /// <summary>当前是否处于按下状态。</summary>
    public bool IsTriggered => isTriggered;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        gameObject.tag = "Button";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag) && !isTriggered)
        {
            isTriggered = true;
            OnButtonTriggered?.Invoke();

            // 一次性按钮：触发后隐藏
            if (isOneTime)
            {
                col.enabled = false;
                Renderer rend = GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 非一次性按钮：离开后释放
        if (other.CompareTag(triggerTag) && isTriggered && !isOneTime)
        {
            isTriggered = false;
            OnButtonReleased?.Invoke();
        }
    }

    /// <summary>重置按钮状态（关卡重置时调用）。</summary>
    public void ResetButton()
    {
        isTriggered = false;
        col.enabled = true;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = true;
    }

    /// <summary>外部强制激活（供 ColorConditionGroup 等调用）。</summary>
    public void ForceActivate()
    {
        if (!isTriggered)
        {
            isTriggered = true;
            OnButtonTriggered?.Invoke();
        }
    }

    /// <summary>外部强制释放。</summary>
    public void ForceDeactivate()
    {
        if (isTriggered)
        {
            isTriggered = false;
            OnButtonReleased?.Invoke();
        }
    }

    // ── IResettable ──
    private bool _savedTriggered;
    private bool _savedColEnabled;
    private bool _savedRendEnabled;

    public void SaveCheckpointState()
    {
        _savedTriggered = isTriggered;
        _savedColEnabled = col.enabled;
        Renderer rend = GetComponent<Renderer>();
        _savedRendEnabled = rend != null && rend.enabled;
    }

    public void ResetToCheckpointState()
    {
        isTriggered = _savedTriggered;
        col.enabled = _savedColEnabled;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = _savedRendEnabled;
    }
}