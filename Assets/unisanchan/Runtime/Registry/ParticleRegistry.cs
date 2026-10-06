using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum ParticleOriginMode
{
    World,
    Target,
    Camera
}

public enum ParticleTransitionMode
{
    None,
    Scale,
    Emission
}

[Serializable]
public class ParticleRegistryEntry
{
    [FormerlySerializedAs("Name")]
    [FormerlySerializedAs("PresetName")]
    [Tooltip("ExcelのArg1から呼び出すParticle名")]
    public string ParticleName;

    [Tooltip("生成するParticle Prefab")]
    public GameObject Prefab;
}

[CreateAssetMenu(
    fileName = "ParticleRegistry",
    menuName = "unisanchan/Particle Registry")]
public class ParticleRegistry : ScriptableObject
{
    [SerializeField]
    private List<ParticleRegistryEntry> entries = new();

    public IReadOnlyList<ParticleRegistryEntry> Entries
        => entries;

    public ParticleRegistryEntry FindPreset(
        string presetName)
    {
        if (entries == null ||
            string.IsNullOrWhiteSpace(presetName))
        {
            return null;
        }

        string normalizedName =
            presetName.Trim();

        foreach (ParticleRegistryEntry entry in entries)
        {
            if (entry == null)
                continue;

            if (string.Equals(
                    entry.ParticleName?.Trim(),
                    normalizedName,
                    StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    // 現在の最小ParticleCommandHandlerとの
    // 一時的な互換性を維持する。
    public GameObject FindParticle(
        string presetName)
    {
        ParticleRegistryEntry preset =
            FindPreset(presetName);

        return preset != null
            ? preset.Prefab
            : null;
    }
}