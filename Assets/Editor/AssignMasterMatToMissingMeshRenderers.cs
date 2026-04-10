using System;
using UnityEditor;
using UnityEngine;

public static class AssignMasterMatToMissingMeshRenderers
{
    const string kTargetMaterialName = "MasterMat";

    [MenuItem("Tools/Futaba/修复 Missing 材质(element0) 为 MasterMat", priority = 2000)]
    static void FixMissingElement0()
    {
        var masterMat = LoadMasterMaterial();
        if (masterMat == null)
        {
            EditorUtility.DisplayDialog(
                "未找到材质",
                $"找不到名为“{kTargetMaterialName}”的 Material 资产。\n" +
                "请确认项目里存在该材质，并且名称完全一致。",
                "OK");
            return;
        }

        var renderers = FindAllMeshRenderersInLoadedScenes();
        var changed = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Assign MasterMat to Missing element0");
        var undoGroup = Undo.GetCurrentGroup();

        foreach (var r in renderers)
        {
            if (r == null)
                continue;

            var mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0)
                continue;

            if (mats[0] != null)
                continue;

            Undo.RecordObject(r, "Assign MasterMat");
            mats[0] = masterMat;
            r.sharedMaterials = mats;
            EditorUtility.SetDirty(r);
            changed++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog(
            "完成",
            $"已处理 MeshRenderer: {renderers.Length}\n" +
            $"已修复 element0 Missing: {changed}",
            "OK");
    }

    static Material LoadMasterMaterial()
    {
        var guids = AssetDatabase.FindAssets($"{kTargetMaterialName} t:Material");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && string.Equals(mat.name, kTargetMaterialName, StringComparison.Ordinal))
                return mat;
        }

        return null;
    }

    static MeshRenderer[] FindAllMeshRenderersInLoadedScenes()
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
#endif
    }
}
