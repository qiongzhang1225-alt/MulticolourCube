using UnityEngine;

public class ResettableCollectible : BaseResettable
{
    private bool isCollected;
    private bool savedCollect;

    protected override void Awake()
    {
        base.Awake();
        isCollected = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            Collect();
        }
    }

    void Collect()
    {
        isCollected = true;
        // 只隐藏，不销毁！（关键修复）
        if (sr != null) sr.enabled = false;
        if (col != null) col.enabled = false;
    }

    public override void SaveCheckpointState()
    {
        base.SaveCheckpointState();
        savedCollect = isCollected;
    }

    public override void ResetToCheckpointState()
    {
        base.ResetToCheckpointState();
        isCollected = savedCollect;
        // 复活后恢复显示
        if (sr != null) sr.enabled = !isCollected;
        if (col != null) col.enabled = !isCollected;
    }
}