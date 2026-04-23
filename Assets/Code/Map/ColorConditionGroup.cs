using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 颜色条件组 —— 多个 ColorChangePlatform 的串联控制器（AND 逻辑）。
///
/// 用法：
///   1. 在场景中创建空物体，挂载此脚本。
///   2. 将需要串联的 ColorChangePlatform 拖入 platforms 列表，
///      或者在各 ColorChangePlatform 上设置 conditionGroup 引用（自动注册）。
///   3. 每个 ColorChangePlatform 需要开启 useColorCondition 并设置 requiredColor。
///   4. 当所有方块的颜色都满足各自要求时，OnAllConditionsMet 触发。
///   5. 任意方块颜色不再满足时，OnConditionBroken 触发。
///
/// 可直接作为 ButtonTriggeredMovablePlatform 的 colorConditionGroup 触发源，
/// 实现"所有颜色正确 → 开门"的效果。
/// </summary>
public class ColorConditionGroup : MonoBehaviour, IResettable
{
    [Header("颜色条件方块列表")]
    [Tooltip("手动拖入，或让方块通过 conditionGroup 字段自动注册")]
    public List<ColorChangePlatform> platforms = new List<ColorChangePlatform>();

    [Header("调试")]
    [Tooltip("满足条件数 / 总数")]
    [SerializeField] private string debugStatus = "";

    /// <summary>所有条件都满足时触发（等效 OnButtonTriggered）。</summary>
    public event Action OnAllConditionsMet;

    /// <summary>条件被打破时触发（等效 OnButtonReleased）。</summary>
    public event Action OnConditionBroken;

    /// <summary>当前是否所有条件都已满足。</summary>
    public bool IsAllMet { get; private set; } = false;

    // 已订阅事件的平台（防止重复订阅）
    private HashSet<ColorChangePlatform> subscribedPlatforms = new HashSet<ColorChangePlatform>();

    private void Start()
    {
        // 对 Inspector 中手动拖入的方块进行订阅
        foreach (var p in platforms)
        {
            if (p != null) SubscribePlatform(p);
        }

        // 初始检查一次
        CheckAllConditions();
    }

    /// <summary>
    /// 注册一个颜色平台到本组（由 ColorChangePlatform.Start 自动调用）。
    /// </summary>
    public void RegisterPlatform(ColorChangePlatform platform)
    {
        if (platform == null) return;

        if (!platforms.Contains(platform))
            platforms.Add(platform);

        SubscribePlatform(platform);
    }

    private void SubscribePlatform(ColorChangePlatform platform)
    {
        if (subscribedPlatforms.Contains(platform)) return;
        subscribedPlatforms.Add(platform);
        platform.OnColorChanged += OnPlatformColorChanged;
    }

    private void OnDestroy()
    {
        foreach (var p in subscribedPlatforms)
        {
            if (p != null)
                p.OnColorChanged -= OnPlatformColorChanged;
        }
        subscribedPlatforms.Clear();
    }

    // ──────── 条件检测 ────────

    private void OnPlatformColorChanged(ColorChangePlatform changed)
    {
        CheckAllConditions();
    }

    /// <summary>重新检查所有平台的颜色条件。</summary>
    public void CheckAllConditions()
    {
        int total = 0;
        int matched = 0;
        bool allMet = true;

        foreach (var p in platforms)
        {
            if (p == null) continue;
            total++;
            if (p.IsColorMatched)
                matched++;
            else
                allMet = false;
        }

        // 没有有效平台时不触发
        if (total == 0) allMet = false;

        // 更新调试信息
        debugStatus = $"{matched}/{total}";

        // 状态变化时触发事件
        if (allMet && !IsAllMet)
        {
            IsAllMet = true;
            Debug.Log($"[ColorGroup] {gameObject.name} ★ 所有条件满足！({matched}/{total})");
            OnAllConditionsMet?.Invoke();
        }
        else if (!allMet && IsAllMet)
        {
            IsAllMet = false;
            Debug.Log($"[ColorGroup] {gameObject.name} ✗ 条件被打破 ({matched}/{total})");
            OnConditionBroken?.Invoke();
        }
    }

    /// <summary>重置所有平台颜色（检查点恢复时调用）。</summary>
    public void ResetAll()
    {
        foreach (var p in platforms)
        {
            if (p != null)
                p.ResetPlatformColor();
        }
        IsAllMet = false;
    }

    // ── IResettable ──
    // ColorConditionGroup 自身只需重置 IsAllMet 标记，
    // 各平台由它们自己的 IResettable 负责恢复颜色。

    public void SaveCheckpointState()
    {
        // 状态由子平台各自保存
    }

    public void ResetToCheckpointState()
    {
        // 子平台各自恢复颜色后，重新检查一次条件
        IsAllMet = false;
        CheckAllConditions();
    }
}
