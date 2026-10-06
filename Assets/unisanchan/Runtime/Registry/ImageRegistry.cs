using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ImageRegistry",
    menuName = "unisanchan/Image Registry")]
public class ImageRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string ImageID;
        public string Type;
        public Sprite Sprite;

        [HideInInspector]
        public string AssetGuid;

        [HideInInspector]
        public long LocalFileId;
    }

    [SerializeField]
    private List<UnityEngine.Object> sourceFolders = new();

    public List<Entry> Entries = new();

    public IReadOnlyList<UnityEngine.Object> SourceFolders
        => sourceFolders;

    public Sprite FindSprite(string imageId)
    {
        if (string.IsNullOrWhiteSpace(imageId))
            return null;

        string normalizedId = imageId.Trim();

        foreach (Entry entry in Entries)
        {
            if (entry == null)
                continue;

            if (string.Equals(
                    entry.ImageID?.Trim(),
                    normalizedId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return entry.Sprite;
            }
        }

        return null;
    }
}