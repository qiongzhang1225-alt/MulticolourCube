using UnityEngine;

/// <summary>
/// 方块面能力基类，所有面专属能力都继承这个类
/// </summary>
public abstract class FaceAbilityBase : MonoBehaviour
{
    [Header("能力基础设置")]
    [Tooltip("能力是否启用")] public bool isAbilityEnabled = true;
    protected BlockController blockController;
    public bool isFaceActive = false; // 当前所在的面是否处于接地激活状态

    protected virtual void Awake()
    {
        // 自动获取父物体的方块控制器
        blockController = GetComponentInParent<BlockController>();
        if (blockController == null)
        {
            Debug.LogError($"面能力{GetType().Name}找不到方块控制器！", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 当所在的面接地时，调用此方法（激活能力）
    /// </summary>
    public virtual void OnFaceActivated()
    {
        if (!isAbilityEnabled) return;
        isFaceActive = true;
        OnAbilityEnable();
    }

    /// <summary>
    /// 当所在的面离开地面时，调用此方法（失活能力）
    /// </summary>
    public virtual void OnFaceDeactivated()
    {
        isFaceActive = false;
        OnAbilityDisable();
    }

    /// <summary>
    /// 能力激活时的逻辑，子类重写
    /// </summary>
    protected abstract void OnAbilityEnable();

    /// <summary>
    /// 能力失活时的逻辑，子类重写
    /// </summary>
    protected abstract void OnAbilityDisable();

    /// <summary>
    /// 能力激活时的每帧更新，子类重写
    /// </summary>
    public virtual void AbilityUpdate() { }
}