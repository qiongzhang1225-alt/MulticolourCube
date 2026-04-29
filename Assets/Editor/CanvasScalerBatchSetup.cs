using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 编辑器批处理工具：
/// 菜单 Tools → Setup Canvas Scalers (All Scenes)
/// 遍历 Build Settings 里的全部场景，给每个 Canvas 挂上 CanvasScalerAutoConfig（若没有），
/// 并把原有 CanvasScaler 的模式统一改为 ScaleWithScreenSize 1920×1080 match=0.5。
/// </summary>
public static class CanvasScalerBatchSetup
{
    [MenuItem("Tools/Setup Canvas Scalers (All Scenes)")]
    public static void BatchSetupAllScenes()
    {
        // 保存当前场景（避免未保存修改丢失）
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string activeScenePath = EditorSceneManager.GetActiveScene().path;
        int processedCanvases = 0;
        int processedScenes   = 0;

        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled) continue;

            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            int countInScene = PatchCanvasesInScene();
            if (countInScene > 0)
            {
                EditorSceneManager.SaveScene(scene);
                processedCanvases += countInScene;
                processedScenes++;
            }
        }

        // 恢复原场景
        if (!string.IsNullOrEmpty(activeScenePath))
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

        Debug.Log($"[CanvasScaler] 批处理完毕：共修改 {processedScenes} 个场景，{processedCanvases} 个 Canvas。");
        EditorUtility.DisplayDialog("Canvas Scaler 配置完成",
            $"已处理 {processedScenes} 个场景，{processedCanvases} 个 Canvas。\n\n" +
            "所有 Canvas 现在使用 Scale With Screen Size (1920×1080, match=0.5)。",
            "OK");
    }

    /// <summary>处理当前已加载场景中的所有 Canvas，返回处理数量。</summary>
    static int PatchCanvasesInScene()
    {
        var canvases = GameObject.FindObjectsOfType<Canvas>(true);
        int count = 0;

        foreach (var canvas in canvases)
        {
            // 只处理根 Canvas（非 World Space 且没有父 Canvas）
            if (canvas.transform.parent != null &&
                canvas.transform.parent.GetComponentInParent<Canvas>() != null)
                continue;
            if (canvas.renderMode == RenderMode.WorldSpace) continue;

            // CanvasScaler：直接设属性（不依赖 CanvasScalerAutoConfig 组件）
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920f, 1080f);
            scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight   = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            EditorUtility.SetDirty(canvas);
            count++;
        }
        return count;
    }

    // ── 仅处理当前场景 ──
    [MenuItem("Tools/Setup Canvas Scalers (Current Scene Only)")]
    public static void SetupCurrentScene()
    {
        int count = PatchCanvasesInScene();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"[CanvasScaler] 当前场景处理了 {count} 个 Canvas。");
        EditorUtility.DisplayDialog("完成", $"当前场景已处理 {count} 个 Canvas。", "OK");
    }
}
