using UnityEngine;

// 继承我们的可重置基类，自动支持复活刷新
public class CollectableStar : BaseResettable
{
    public static int CollectedCount = 0;

    // 自定义状态
    private bool isCollected = false;
    private bool savedIsCollected; // 检查点存档的状态

    // 重写Awake，调用基类初始化
    protected override void Awake()
    {
        base.Awake();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 未收集 + 碰到玩家
        if (!isCollected && other.CompareTag("Player"))
        {
            CollectStar();
        }
    }

    // 收集逻辑（不销毁！只隐藏）
    void CollectStar()
    {
        isCollected = true;
        CollectedCount++;

        // 隐藏星星，不销毁（关键修复）
        if (sr != null) sr.enabled = false;
        if (col != null) col.enabled = false;

        Debug.Log("收集了一颗星星，当前总数：" + CollectedCount);
    }

    // 检查点存档：保存收集状态
    public override void SaveCheckpointState()
    {
        base.SaveCheckpointState();
        savedIsCollected = isCollected;
    }

    // 复活重置：恢复状态 + 修正计数
    public override void ResetToCheckpointState()
    {
        base.ResetToCheckpointState();

        // 如果复活后需要变回【未收集】
        if (isCollected && !savedIsCollected)
        {
            CollectedCount--; // 退回计数（核心！防刷）
        }

        // 恢复状态
        isCollected = savedIsCollected;

        // 复活后显示/隐藏星星
        if (sr != null) sr.enabled = !isCollected;
        if (col != null) col.enabled = !isCollected;
    }
}