#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public static class XlsxToJsonConverter
{
    private const int CommentColumnIndex = 0;

    private static readonly string[] KnownColumns =
    {
        "EventID",
        "BlockID",
        "Trigger",
        "Command",

        "Arg1",
        "Arg2",
        "Arg3",
        "Arg4",
        "Arg5",
        "Arg6",
        "Arg7",
        "Arg8",
        "Arg9",
        "Arg10",
        "Arg11",
        "Arg12",
        
        "Speaker",
        "Text",
        "Voice",
        "HintText",
        "DebugLog",
        "ExcelRow"
    };

    private static readonly string[] CameraPresetColumns =
    {
        "PresetName",
        "PosX",
        "PosY",
        "PosZ",
        "RotX",
        "RotY",
        "RotZ",
        "FOV",
        "Space",
        "Duration",
        "EasingType"
    };

    private static readonly string[] ParticlePresetColumns =
    {
        "PresetName",
        "OriginMode",
        "TargetId",
        "AnchorId",

        "PosX",
        "PosY",
        "PosZ",

        "RotX",
        "RotY",
        "RotZ",

        "ScaleX",
        "ScaleY",
        "ScaleZ",

        "Follow"
    };

    private static readonly string[] ImageColumns =
    {
        "ImageID",
        "Type",
        "FilePath"
    };

    private static readonly string[] ModalPresetColumns =
    {
        "PresetID",
        "Page",
        "ImageID",
        "Title",
        "Caption",
        "Body",
        "InputHint"
    };

    [MenuItem("Tools/Excel Event System/Convert Xlsx To Json")]
    public static void ConvertXlsxToJson()
    {
        EventSystemSettings settings = null;

        foreach (EventSystemSettings candidate
                in Resources.FindObjectsOfTypeAll<EventSystemSettings>())
        {
            if (candidate == null ||
                EditorUtility.IsPersistent(candidate) ||
                !candidate.gameObject.scene.IsValid())
                continue;

            settings = candidate;
            break;
        }

        if (settings == null)
        {
            Debug.LogWarning(
                "[Excel Event System] EventSystemSettings が見つかりません。");
            return;
        }

        if (settings.ValidExcelFileCount == 0)
        {
            Debug.LogWarning(
                "[Excel Event System] Scenario Excel Files が登録されていません。");
            return;
        }

        foreach (DefaultAsset excelAsset in settings.GetValidExcelFiles())
        {
            string assetPath = AssetDatabase.GetAssetPath(excelAsset);

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Debug.LogWarning(
                    "[Excel Event System] Excelファイルのパスを取得できませんでした。");
                continue;
            }

            string xlsxPath = Path.GetFullPath(assetPath);
            string jsonPath = Path.ChangeExtension(xlsxPath, ".json");

            if (!File.Exists(xlsxPath))
            {
                Debug.LogWarning(
                    "[Excel Event System] XLSXファイルが見つかりません: " + xlsxPath);
                continue;
            }

            List<Dictionary<string, string>> rows =
                ReadSheet(
                    xlsxPath,
                    "xl/worksheets/sheet1.xml",
                    KnownColumns);

            string cameraPresetSheetPath =
                GetSheetPathByName(xlsxPath, "CameraPreset");

            List<Dictionary<string, string>> cameraPresets =
                ReadSheet(
                    xlsxPath,
                    cameraPresetSheetPath,
                    CameraPresetColumns);

            string particlePresetSheetPath =
                GetSheetPathByName(
                    xlsxPath,
                    "ParticlePreset");

            List<Dictionary<string, string>> particlePresets =
                ReadSheet(
                    xlsxPath,
                    particlePresetSheetPath,
                    ParticlePresetColumns);
            
            string imageSheetPath =
                GetSheetPathByName(xlsxPath, "Image");

            List<Dictionary<string, string>> images =
                ReadSheet(
                    xlsxPath,
                    imageSheetPath,
                    ImageColumns);

            string modalPresetSheetPath =
                GetSheetPathByName(
                    xlsxPath,
                    "ModalPreset");

            List<Dictionary<string, string>> modalPresets =
                ReadSheet(
                    xlsxPath,
                    modalPresetSheetPath,
                    ModalPresetColumns);

            string json =
                BuildJson(
                    rows,
                    cameraPresets,
                    particlePresets,
                    images);

            File.WriteAllText(
                jsonPath,
                json,
                new UTF8Encoding(false));
            
            string jsonAssetPath =
                assetPath.Replace(".xlsx", ".json");

            AssetDatabase.ImportAsset(
                jsonAssetPath,
                ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.Refresh();
    }

    private static string GetSheetPathByName(
        string xlsxPath,
        string sheetName)
    {
        using FileStream fs = new FileStream(
            xlsxPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read);

        XNamespace mainNs =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        XNamespace relNs =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        XNamespace packageRelNs =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        ZipArchiveEntry workbookEntry =
            archive.GetEntry("xl/workbook.xml");

        ZipArchiveEntry relsEntry =
            archive.GetEntry("xl/_rels/workbook.xml.rels");

        if (workbookEntry == null || relsEntry == null)
            return null;

        XDocument workbookDoc;
        using (Stream stream = workbookEntry.Open())
        {
            workbookDoc = XDocument.Load(stream);
        }

        XElement sheet = null;

        foreach (XElement element in workbookDoc.Descendants(mainNs + "sheet"))
        {
            string name = element.Attribute("name")?.Value;

            if (name == sheetName)
            {
                sheet = element;
                break;
            }
        }

        if (sheet == null)
            return null;

        string relationshipId =
            sheet.Attribute(relNs + "id")?.Value;

        if (string.IsNullOrWhiteSpace(relationshipId))
            return null;

        XDocument relsDoc;
        using (Stream stream = relsEntry.Open())
        {
            relsDoc = XDocument.Load(stream);
        }

        foreach (XElement relationship in
                relsDoc.Descendants(packageRelNs + "Relationship"))
        {
            string id = relationship.Attribute("Id")?.Value;

            if (id != relationshipId)
                continue;

            string target = relationship.Attribute("Target")?.Value;

            if (string.IsNullOrWhiteSpace(target))
                return null;

            target = target.Replace("\\", "/");

            if (target.StartsWith("/"))
                return target.TrimStart('/');

            return "xl/" + target;
        }

        return null;
    }

    private static List<Dictionary<string, string>> ReadSheet(
        string xlsxPath,
        string sheetPath,
        string[] meaningfulColumns)
    {
        List<Dictionary<string, string>> result =
            new List<Dictionary<string, string>>();

        // 対象シートがないExcelでは、空データとして扱う。
        if (string.IsNullOrWhiteSpace(sheetPath))
            return result;

        using FileStream fs = new FileStream(
            xlsxPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read);

        List<string> sharedStrings = ReadSharedStrings(archive);
        ZipArchiveEntry sheetEntry = archive.GetEntry(sheetPath);

        if (sheetEntry == null)
        {
            Debug.LogError("sheet1.xml not found in xlsx.");
            return result;
        }

        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument sheetDoc;

        using (Stream stream = sheetEntry.Open())
        {
            sheetDoc = XDocument.Load(stream);
        }

        List<string> headers = new List<string>();

        foreach (XElement row in sheetDoc.Descendants(ns + "row"))
        {
            int excelRow =
                int.Parse(row.Attribute("r").Value);

            Dictionary<int, string> cellValues = new Dictionary<int, string>();

            foreach (XElement cell in row.Elements(ns + "c"))
            {
                string cellRef = cell.Attribute("r")?.Value;
                int colIndex = GetColumnIndex(cellRef);
                string value = GetCellValue(cell, sharedStrings, ns);
                cellValues[colIndex] = value;
            }

            if (headers.Count == 0)
            {
                int maxCol = GetMaxColumn(cellValues);

                for (int i = 0; i <= maxCol; i++)
                {
                    cellValues.TryGetValue(i, out string header);
                    headers.Add(header ?? "");
                }

                continue;
            }

            Dictionary<string, string> rowData = new Dictionary<string, string>();

            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i];

                if (string.IsNullOrWhiteSpace(header))
                    continue;

                cellValues.TryGetValue(i, out string value);
                rowData[header] = value ?? "";
            }

            // 一番左の列(A列)が「//」で始まっていたらコメントアウト行として無視
            cellValues.TryGetValue(CommentColumnIndex, out string comment);

            if (!string.IsNullOrWhiteSpace(comment) &&
                comment.TrimStart().StartsWith("//"))
            {
                continue;
            }

            if (rowData.Count > 0 &&
                IsMeaningfulRow(rowData, meaningfulColumns))
            {
                // シナリオとして有効な行だけ、元のExcel行番号を記録する
                rowData["ExcelRow"] = excelRow.ToString();

                result.Add(rowData);
            }
        }

        return result;
    }

    private static bool IsMeaningfulRow(Dictionary<string, string> rowData,string[] meaningfulColumns)
    {
        foreach (string key in meaningfulColumns)
        {
            if (rowData.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value))
                return true;
        }

        return false;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        List<string> result = new List<string>();
        ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");

        if (entry == null)
            return result;

        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument doc;

        using (Stream stream = entry.Open())
        {
            doc = XDocument.Load(stream);
        }

        foreach (XElement si in doc.Descendants(ns + "si"))
        {
            StringBuilder text = new StringBuilder();
            XElement directText = si.Element(ns + "t");

            if (directText != null)
            {
                text.Append(directText.Value);
            }
            else
            {
                foreach (XElement r in si.Elements(ns + "r"))
                {
                    XElement t = r.Element(ns + "t");
                    if (t != null)
                        text.Append(t.Value);
                }
            }

            result.Add(text.ToString());
        }

        return result;
    }

    private static string GetCellValue(XElement cell, List<string> sharedStrings, XNamespace ns)
    {
        string type = cell.Attribute("t")?.Value;

        if (type == "s")
        {
            XElement v = cell.Element(ns + "v");
            if (v == null)
                return "";

            if (int.TryParse(v.Value, out int index) && index >= 0 && index < sharedStrings.Count)
                return sharedStrings[index];

            return "";
        }

        if (type == "inlineStr")
        {
            XElement t = cell.Element(ns + "is")?.Element(ns + "t");
            return t?.Value ?? "";
        }

        return cell.Element(ns + "v")?.Value ?? "";
    }

    private static int GetColumnIndex(string cellRef)
    {
        if (string.IsNullOrEmpty(cellRef))
            return 0;

        Match match = Regex.Match(cellRef, @"^[A-Z]+");
        if (!match.Success)
            return 0;

        string letters = match.Value;
        int index = 0;

        foreach (char c in letters)
        {
            index *= 26;
            index += c - 'A' + 1;
        }

        return index - 1;
    }

    private static int GetMaxColumn(Dictionary<int, string> cellValues)
    {
        int max = 0;
        foreach (int key in cellValues.Keys)
            if (key > max) max = key;
        return max;
    }

    private static string BuildJson(
        List<Dictionary<string,string>> rows,
        List<Dictionary<string,string>> cameraPresets,
        List<Dictionary<string,string>> particlePresets,
        List<Dictionary<string,string>> images)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"rows\": [");

        for (int i = 0; i < rows.Count; i++)
        {
            Dictionary<string, string> row = rows[i];
            sb.AppendLine("    {");

            for (int j = 0; j < KnownColumns.Length; j++)
            {
                string key = KnownColumns[j];
                row.TryGetValue(key, out string value);

                sb.Append("      ");
                sb.Append("\"");
                sb.Append(key);
                sb.Append("\": ");
                sb.Append("\"");
                sb.Append(EscapeJson(value ?? ""));
                sb.Append("\"");

                if (j < KnownColumns.Length - 1)
                    sb.Append(",");

                sb.AppendLine();
            }

            sb.Append("    }");
            if (i < rows.Count - 1)
                sb.Append(",");
            sb.AppendLine();
        }

        sb.AppendLine("  ],");
        sb.AppendLine("  \"cameraPresets\": [");

        for (int i = 0; i < cameraPresets.Count; i++)
{
    Dictionary<string, string> preset = cameraPresets[i];

    sb.AppendLine("    {");

        for (int j = 0; j < CameraPresetColumns.Length; j++)
            {
                string key = CameraPresetColumns[j];

                preset.TryGetValue(key, out string value);

                sb.Append("      ");
                sb.Append("\"");
                sb.Append(key);
                sb.Append("\": ");
                sb.Append("\"");
                sb.Append(EscapeJson(value ?? ""));
                sb.Append("\"");

                if (j < CameraPresetColumns.Length - 1)
                    sb.Append(",");

                sb.AppendLine();
            }

            sb.Append("    }");

            if (i < cameraPresets.Count - 1)
                sb.Append(",");

            sb.AppendLine();
        }

        sb.AppendLine("  ],");
        sb.AppendLine("  \"particlePresets\": [");

        for (int i = 0; i < particlePresets.Count; i++)
        {
            Dictionary<string, string> preset =
                particlePresets[i];

            sb.AppendLine("    {");

            for (int j = 0;
                j < ParticlePresetColumns.Length;
                j++)
            {
                string key =
                    ParticlePresetColumns[j];

                preset.TryGetValue(
                    key,
                    out string value);

                sb.Append("      ");
                sb.Append("\"");
                sb.Append(key);
                sb.Append("\": ");
                sb.Append("\"");
                sb.Append(
                    EscapeJson(value ?? ""));
                sb.Append("\"");

                if (j <
                    ParticlePresetColumns.Length - 1)
                {
                    sb.Append(",");
                }

                sb.AppendLine();
            }

            sb.Append("    }");

            if (i < particlePresets.Count - 1)
            {
                sb.Append(",");
            }

            sb.AppendLine();
        }

        sb.AppendLine("  ],");
        sb.AppendLine("  \"images\": [");

        for (int i = 0; i < images.Count; i++)
        {
            Dictionary<string, string> image = images[i];

            sb.AppendLine("    {");

            for (int j = 0; j < ImageColumns.Length; j++)
            {
                string key = ImageColumns[j];

                image.TryGetValue(key, out string value);

                sb.Append("      ");
                sb.Append("\"");
                sb.Append(key);
                sb.Append("\": ");
                sb.Append("\"");
                sb.Append(EscapeJson(value ?? ""));
                sb.Append("\"");

                if (j < ImageColumns.Length - 1)
                    sb.Append(",");

                sb.AppendLine();
            }

            sb.Append("    }");

            if (i < images.Count - 1)
                sb.Append(",");

            sb.AppendLine();
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public static void ConvertPresetXlsxToJson()
    {
        const string registryPath =
            "Assets/unisanchan/Data/PresetRegistry.asset";

        PresetRegistry registry =
            AssetDatabase.LoadAssetAtPath<PresetRegistry>(
                registryPath);

        if (registry == null)
        {
            Debug.LogError(
                "[unisanchan] PresetRegistry.asset が見つかりません。");
            return;
        }

        List<TextAsset> generatedSources =
            new List<TextAsset>();

        foreach (DefaultAsset excelAsset in registry.ExcelSources)
        {
            if (excelAsset == null)
                continue;

            string assetPath =
                AssetDatabase.GetAssetPath(excelAsset);

            if (string.IsNullOrWhiteSpace(assetPath))
                continue;

            string xlsxPath =
                Path.GetFullPath(assetPath);

            if (!File.Exists(xlsxPath))
            {
                Debug.LogWarning(
                    "[unisanchan] Preset XLSXが見つかりません: "
                    + assetPath);

                continue;
            }

            string modalPresetSheetPath =
                GetSheetPathByName(
                    xlsxPath,
                    "ModalPreset");

            if (string.IsNullOrWhiteSpace(
                    modalPresetSheetPath))
            {
                Debug.LogError(
                    "[unisanchan] Preset Excel「"
                    + assetPath
                    + "」にModalPresetシートがありません。");

                continue;
            }

            List<Dictionary<string, string>> modalPresets =
                ReadSheet(
                    xlsxPath,
                    modalPresetSheetPath,
                    ModalPresetColumns);

            string cameraPresetSheetPath =
                GetSheetPathByName(
                    xlsxPath,
                    "CameraPreset");

            List<Dictionary<string, string>> cameraPresets =
                ReadSheet(
                    xlsxPath,
                    cameraPresetSheetPath,
                    CameraPresetColumns);

            string particlePresetSheetPath =
                GetSheetPathByName(
                    xlsxPath,
                    "ParticlePreset");

            List<Dictionary<string, string>> particlePresets =
                ReadSheet(
                    xlsxPath,
                    particlePresetSheetPath,
                    ParticlePresetColumns);

            string json =
                BuildPresetJson(
                    cameraPresets,
                    particlePresets,
                    modalPresets);

            string jsonAssetPath =
                Path.ChangeExtension(
                        assetPath,
                        ".json")
                    .Replace("\\", "/");

            string jsonFullPath =
                Path.GetFullPath(jsonAssetPath);

            File.WriteAllText(
                jsonFullPath,
                json,
                new UTF8Encoding(false));

            AssetDatabase.ImportAsset(
                jsonAssetPath,
                ImportAssetOptions.ForceUpdate);

            TextAsset generatedSource =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    jsonAssetPath);

            if (generatedSource != null)
                generatedSources.Add(generatedSource);
            
            Debug.Log(
                "[unisanchan] Preset JSONを更新しました: "
                + jsonAssetPath
                + " / Camera: "
                + cameraPresets.Count
                + " / Particle: "
                + particlePresets.Count
                + " / Modal: "
                + modalPresets.Count);
        }

        registry.SetGeneratedSources(generatedSources);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static string BuildPresetJson(
        List<Dictionary<string, string>> cameraPresets,
        List<Dictionary<string, string>> particlePresets,
        List<Dictionary<string, string>> modalPresets)
    {
        StringBuilder sb =
            new StringBuilder();

        sb.AppendLine("{");

        AppendPresetArray(
            sb,
            "cameraPresets",
            cameraPresets,
            CameraPresetColumns,
            true);

        AppendPresetArray(
            sb,
            "particlePresets",
            particlePresets,
            ParticlePresetColumns,
            true);

        AppendPresetArray(
            sb,
            "modalPresets",
            modalPresets,
            ModalPresetColumns,
            false);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AppendPresetArray(
        StringBuilder sb,
        string arrayName,
        List<Dictionary<string, string>> entries,
        string[] columns,
        bool appendComma)
    {
        sb.Append("  \"");
        sb.Append(arrayName);
        sb.AppendLine("\": [");

        for (int i = 0; i < entries.Count; i++)
        {
            Dictionary<string, string> entry =
                entries[i];

            sb.AppendLine("    {");

            for (int j = 0; j < columns.Length; j++)
            {
                string key =
                    columns[j];

                entry.TryGetValue(
                    key,
                    out string value);

                sb.Append("      \"");
                sb.Append(key);
                sb.Append("\": \"");
                sb.Append(
                    EscapeJson(value ?? ""));
                sb.Append("\"");

                if (j < columns.Length - 1)
                    sb.Append(",");

                sb.AppendLine();
            }

            sb.Append("    }");

            if (i < entries.Count - 1)
                sb.Append(",");

            sb.AppendLine();
        }

        sb.Append("  ]");

        if (appendComma)
            sb.Append(",");

        sb.AppendLine();
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }
}
#endif
