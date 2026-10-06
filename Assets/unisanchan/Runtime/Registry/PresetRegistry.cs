using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(
    fileName = "PresetRegistry",
    menuName = "unisanchan/Preset Registry")]
public class PresetRegistry : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField]
    private List<DefaultAsset> excelSources = new();

    public IReadOnlyList<DefaultAsset> ExcelSources
        => excelSources;
#endif

    [SerializeField]
    private List<TextAsset> sources = new();

    public IReadOnlyList<TextAsset> Sources
        => sources;

#if UNITY_EDITOR
    public void SetGeneratedSources(
        List<TextAsset> generatedSources)
    {
        sources =
            generatedSources
            ?? new List<TextAsset>();

        EditorUtility.SetDirty(this);
    }
#endif
}