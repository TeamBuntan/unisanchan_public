using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AudioRegistryEntry
{
    public string Name;
    public AudioClip Clip;
}

[CreateAssetMenu(
    fileName = "AudioRegistry",
    menuName = "unisanchan/Audio Registry")]
public class AudioRegistry : ScriptableObject
{
    [SerializeField]
    private List<AudioRegistryEntry> bgmEntries = new();
    [SerializeField]
    private List<AudioRegistryEntry> ambienceEntries = new();
    [SerializeField]
    private List<AudioRegistryEntry> seEntries = new();

    [SerializeField, Min(1)]
    private int maxSimultaneousSe = 12;

    public IReadOnlyList<AudioRegistryEntry> BgmEntries
        => bgmEntries;
    
    public IReadOnlyList<AudioRegistryEntry> AmbienceEntries
        => ambienceEntries;

    public IReadOnlyList<AudioRegistryEntry> SeEntries
        => seEntries;

    public int MaxSimultaneousSe
        => Mathf.Max(1, maxSimultaneousSe);

    public AudioClip FindBgm(string bgmName)
    {
        return FindClip(
            bgmEntries,
            bgmName);
    }

    public AudioClip FindAmbience(string ambienceName)
    {
        return FindClip(
            ambienceEntries,
            ambienceName);
    }

    public AudioClip FindSe(string seName)
    {
        return FindClip(
            seEntries,
            seName);
    }

    private static AudioClip FindClip(
        List<AudioRegistryEntry> entries,
        string audioName)
    {
        if (entries == null ||
            string.IsNullOrWhiteSpace(audioName))
        {
            return null;
        }

        string normalizedName =
            audioName.Trim();

        foreach (AudioRegistryEntry entry in entries)
        {
            if (entry == null)
                continue;

            if (string.Equals(
                    entry.Name?.Trim(),
                    normalizedName,
                    StringComparison.Ordinal))
            {
                return entry.Clip;
            }
        }

        return null;
    }
}