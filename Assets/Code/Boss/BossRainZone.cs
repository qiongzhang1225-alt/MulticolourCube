using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Boss 关卡 — 上空随机掉落区。
///
/// 在场景中放一个空 GameObject 挂本组件，调整 zoneSize 形成一个矩形区域。
/// 把任意 prefab 拖入 entries 列表（可单项设权重），脚本会按 spawnInterval
/// 随机挑一个 prefab 在区域内随机点位生成。生成对象自带 Rigidbody2D 即可掉落。
///
/// 同时暴露 GetRandomPosition() —— BossArenaController 可借此把 ColorBall
/// 也从同一区域内的随机点掉下来，实现"球与杂物一起从天而降"的效果。
/// </summary>
public class BossRainZone : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public GameObject prefab;
        [Range(0f, 10f)]
        [Tooltip("加权随机：值越大越容易出现；0 = 不会出")]
        public float weight = 1f;
    }

    [Header("掉落区域（local 尺寸，以本物体位置为中心）")]
    [Tooltip("矩形区域宽（X）和高（Y）。一般高度设很小，让物体只在水平线上随机生成。")]
    public Vector2 zoneSize = new Vector2(10f, 0.5f);

    [Tooltip("是否在 Y 范围内也随机；false 则统一从 zoneSize 中线生成")]
    public bool randomY = false;

    [Header("掉落物清单")]
    public List<Entry> entries = new List<Entry>();

    [Header("自动掉落节奏")]
    [Tooltip("是否定时自动生成（关闭后只能由外部 SpawnRandom() 触发）")]
    public bool autoSpawn = true;
    [Tooltip("两次掉落之间的间隔（秒）")] public float spawnInterval = 3f;
    [Tooltip("启动延迟（秒）")] public float startupDelay = 1f;

    [Header("数量上限")]
    [Tooltip("本区生成的、当前还活着的物体超过此数则跳过本次生成。<=0 表示不限")]
    public int maxAlive = 6;

    [Header("调试")]
    [Tooltip("Scene 视图绘制区域线框")] public bool drawGizmo = true;
    public Color gizmoColor = new Color(1f, 0.85f, 0.2f, 0.7f);
    public bool verboseLog = false;

    // ── 运行时 ──
    private float nextSpawnTime;
    private readonly List<GameObject> alive = new List<GameObject>();

    void Start()
    {
        nextSpawnTime = Time.time + Mathf.Max(0f, startupDelay);
    }

    void Update()
    {
        if (!autoSpawn) return;
        if (Time.time < nextSpawnTime) return;
        nextSpawnTime = Time.time + Mathf.Max(0.05f, spawnInterval);
        SpawnRandom();
    }

    /// <summary>立即从清单里随机挑一个生成；返回新对象（可能为空）。</summary>
    public GameObject SpawnRandom()
    {
        // 清理已销毁的 tracker 槽
        for (int i = alive.Count - 1; i >= 0; i--)
            if (alive[i] == null) alive.RemoveAt(i);

        if (maxAlive > 0 && alive.Count >= maxAlive)
        {
            if (verboseLog) Debug.Log("[BossRainZone] 数量满，跳过本次掉落");
            return null;
        }

        var prefab = PickWeightedPrefab();
        if (prefab == null) return null;

        Vector3 pos = GetRandomPosition();
        var go = Instantiate(prefab, pos, Quaternion.identity);
        alive.Add(go);

        if (verboseLog) Debug.Log($"[BossRainZone] 生成 {prefab.name} @ {pos}");
        return go;
    }

    /// <summary>外部调用：取本区域内一个随机世界点（用于 ColorBall 等手动定位）。</summary>
    public Vector3 GetRandomPosition()
    {
        float halfW = zoneSize.x * 0.5f;
        float halfH = zoneSize.y * 0.5f;
        float x = Random.Range(-halfW, halfW);
        float y = randomY ? Random.Range(-halfH, halfH) : 0f;
        return transform.position + new Vector3(x, y, 0f);
    }

    GameObject PickWeightedPrefab()
    {
        if (entries == null || entries.Count == 0) return null;

        float total = 0f;
        foreach (var e in entries)
            if (e != null && e.prefab != null && e.weight > 0f) total += e.weight;
        if (total <= 0f) return null;

        float r = Random.Range(0f, total);
        float acc = 0f;
        foreach (var e in entries)
        {
            if (e == null || e.prefab == null || e.weight <= 0f) continue;
            acc += e.weight;
            if (r <= acc) return e.prefab;
        }
        // 浮点兜底
        foreach (var e in entries)
            if (e != null && e.prefab != null && e.weight > 0f) return e.prefab;
        return null;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, new Vector3(zoneSize.x, zoneSize.y, 0.05f));
        // 中线提示（randomY=false 时所有掉落都从这条线开始）
        if (!randomY)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Vector3 left = transform.position + new Vector3(-zoneSize.x * 0.5f, 0f, 0f);
            Vector3 right = transform.position + new Vector3(zoneSize.x * 0.5f, 0f, 0f);
            Gizmos.DrawLine(left, right);
        }
    }
}
