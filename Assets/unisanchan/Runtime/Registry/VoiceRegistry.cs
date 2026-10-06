using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "VoiceRegistry",
    menuName = "unisanchan/Voice Registry")]
public class VoiceRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string VoicePath;
        public AudioClip Clip;
    }

    public List<Entry> Entries = new List<Entry>();

    public AudioClip FindClip(string voicePath)
    {
        if (string.IsNullOrWhiteSpace(voicePath))
            return null;

        foreach (Entry entry in Entries)
        {
            if (entry == null)
                continue;

            if (entry.VoicePath == voicePath)
                return entry.Clip;
        }

        return null;
    }
}