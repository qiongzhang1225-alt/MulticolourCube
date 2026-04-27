using UnityEngine;

public class PeriodicRespawner : MonoBehaviour
{
    [Header("生成设置")]
    [Tooltip("要生成的预制体（如圆石头）")]
    public GameObject prefabToSpawn;

    [Tooltip("生成位置（默认使用当前物体的位置）")]
    public Transform spawnPoint;

    [Tooltip("检查并生成的间隔（秒）")]
    public float respawnInterval = 5f;

    [Tooltip("场景中同时存在的最大数量（通常为1）")]
    public int maxInstances = 1;

    private void Start()
    {
        // 如果没有指定生成点，就用自身位置
        if (spawnPoint == null)
            spawnPoint = transform;

        // 启动时立即尝试生成一次，之后周期性检查
        TrySpawn();
        InvokeRepeating(nameof(TrySpawn), respawnInterval, respawnInterval);
    }

    private void TrySpawn()
    {
        // 已存在的实例数（通过标签或查找所有该预制体实例）
        int currentCount = CountExistingInstances();

        // 如果数量未达到上限，则生成
        if (currentCount < maxInstances)
        {
            Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        }
    }

    private int CountExistingInstances()
    {
        // 方法1：如果预制体有唯一标签（推荐）
        // 在编辑器中给预制体设一个标签，例如 "Boulder"
        // return GameObject.FindGameObjectsWithTag("Boulder").Length;

        // 方法2：如果没有标签，可以通过名称或组件判断（这里用名称示例）
        // 注意：FindObjectsOfType 性能开销大，但低频调用（几秒一次）没问题
        if (prefabToSpawn == null) return 0;

        // 移除 "(Clone)" 后缀，确保名称匹配
        string prefabName = prefabToSpawn.name.Replace("(Clone)", "");
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int count = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Replace("(Clone)", "") == prefabName)
                count++;
        }
        return count;
    }
}