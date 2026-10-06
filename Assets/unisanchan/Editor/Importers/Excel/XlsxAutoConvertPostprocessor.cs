#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;

public class XlsxAutoConvertPostprocessor : AssetPostprocessor
{
    private static bool conversionScheduled;

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool xlsxWasImported =
            Array.Exists(
                importedAssets,
                IsConvertibleXlsx);

        if (!xlsxWasImported ||
            conversionScheduled)
        {
            return;
        }

        conversionScheduled = true;

        EditorApplication.delayCall +=
            RunConversion;
    }

    private static bool IsConvertibleXlsx(
        string assetPath)
    {
        if (!assetPath.EndsWith(
            ".xlsx",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string fileName =
            Path.GetFileName(assetPath);

        // Excel編集中に作られる一時ファイルを除外
        return !fileName.StartsWith(
            "~$",
            StringComparison.Ordinal);
    }

    private static void RunConversion()
    {
        conversionScheduled = false;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        XlsxToJsonConverter.ConvertXlsxToJson();
        XlsxToJsonConverter.ConvertPresetXlsxToJson();
    }
}

#endif