using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EventSystemSettings : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Scenario Excel Files")]
    [Tooltip(
        "3Dイベントで使用するExcelファイルを登録します。\n" +
        "Projectウィンドウから.xlsxファイルをドラッグ＆ドロップしてください。\n" +
        "登録が0件でもゲームは停止しません。")]
    [SerializeField]
    private List<DefaultAsset> scenarioExcelFiles = new();

    public IReadOnlyList<DefaultAsset> ScenarioExcelFiles
        => scenarioExcelFiles;
    
    [Header("Voice")]

    [Tooltip(
        "Voiceファイルを格納する基準フォルダを指定します。\n" +
        "ExcelのVoice列には、このフォルダ以下の相対パスを入力します。")]
    [SerializeField]
    private DefaultAsset voiceRootFolder;

    public DefaultAsset VoiceRootFolder
        => voiceRootFolder;

    public int ValidExcelFileCount
    {
        get
        {
            if (scenarioExcelFiles == null)
                return 0;

            int count = 0;

            foreach (DefaultAsset excelFile in scenarioExcelFiles)
            {
                if (excelFile != null)
                    count++;
            }

            return count;
        }
    }

    public IEnumerable<DefaultAsset> GetValidExcelFiles()
    {
        if (scenarioExcelFiles == null)
            yield break;

        foreach (DefaultAsset excelFile in scenarioExcelFiles)
        {
            if (excelFile != null)
                yield return excelFile;
        }
    }
#endif
}