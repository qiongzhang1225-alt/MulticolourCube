using UnityEngine;

// 多帧序列动画金币 | 兼容复活系统 | 独立计数 | 无奇怪旋转
public class CollectableCoin : BaseResettable
{
    [Header("金币序列帧动画")]
    public Sprite[] coinFrames;       // 把你的多切金币帧全拖进来
    public float frameRate = 0.1f;    // 动画播放速度

    // 独立计数（适配你的 VictoryUI）
    public static int CollectedCount = 0;

    private bool isCollected = false;
    private bool savedIsCollected;
    private SpriteRenderer sr;
    private float frameTimer;
    private int currentFrame;

    protected override void Awake()
    {
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 未收集 + 有动画帧时才播放
        if (!isCollected && coinFrames != null && coinFrames.Length > 0)
        {
            PlayCoinAnimation();
        }
    }

    // 金币序列帧动画（正常金币动画，不旋转物体）
    void PlayCoinAnimation()
    {
        frameTimer += Time.deltaTime;
        if (frameTimer >= frameRate)
        {
            frameTimer = 0;
            currentFrame = (currentFrame + 1) % coinFrames.Length;
            sr.sprite = coinFrames[currentFrame];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCollected && other.CompareTag("Player"))
        {
            Collect();
        }
    }

    void Collect()
    {
        isCollected = true;
        CollectedCount++;

        // 收集后隐藏
        if (sr != null) sr.enabled = false;
        if (col != null) col.enabled = false;
    }

    // 检查点存档
    public override void SaveCheckpointState()
    {
        base.SaveCheckpointState();
        savedIsCollected = isCollected;
    }

    // 复活重置
    public override void ResetToCheckpointState()
    {
        base.ResetToCheckpointState();

        // 防刷回滚计数
        if (isCollected && !savedIsCollected)
        {
            CollectedCount--;
        }

        // 恢复状态与动画
        isCollected = savedIsCollected;
        if (sr != null) sr.enabled = !isCollected;
        if (col != null) col.enabled = !isCollected;

        // 复活重置动画帧
        frameTimer = 0;
        currentFrame = 0;
        if (coinFrames != null && coinFrames.Length > 0)
            sr.sprite = coinFrames[0];
    }
}