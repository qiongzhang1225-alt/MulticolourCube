using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Checkpoint : MonoBehaviour
{
    public int order;
    private List<IResettable> resettableObjects = new List<IResettable>();
    private bool isActivated = false;
    private SpriteRenderer sprite;

    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        // 收集物体 + 自动过滤空对象/已销毁物体
        resettableObjects = FindObjectsOfType<MonoBehaviour>(true)
            .OfType<IResettable>()
            .Where(x => x != null && (x as MonoBehaviour) != null)
            .ToList();

        Debug.Log($"【检查点】找到 {resettableObjects.Count} 个可重置物体！", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated || !other.CompareTag("Player")) return;

        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
        if (respawn != null)
        {
            Debug.Log("【检查点】触发成功！开始存档...", this);
            SaveAllObjectStates();
            respawn.UpdateCheckpoint(transform, order, this);
            isActivated = true;
            if (sprite != null) sprite.color = Color.green;
        }
    }

    // 存档前过滤空对象
    public void SaveAllObjectStates()
    {
        foreach (var obj in resettableObjects)
        {
            if (obj == null || (obj as MonoBehaviour) == null) continue;
            obj.SaveCheckpointState();
        }
        Debug.Log("【检查点】所有物体存档完成！", this);
    }

    // 重置前过滤空对象
    public void ResetAllObjectStates()
    {
        Debug.Log("【检查点】执行重置！", this);
        foreach (var obj in resettableObjects)
        {
            if (obj == null || (obj as MonoBehaviour) == null) continue;
            obj.ResetToCheckpointState();
        }
    }
}