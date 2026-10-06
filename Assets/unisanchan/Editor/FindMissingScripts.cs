using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts In Open Scenes")]
    public static void FindInOpenScenes()
    {
        int totalMissing = 0;

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                totalMissing += ScanGameObjectRecursive(root, scene.name);
            }
        }

        if (totalMissing == 0)
            Debug.Log("Missing Script は見つかりませんでした。");
        else
            Debug.Log($"Missing Script 合計: {totalMissing}");
    }

    private static int ScanGameObjectRecursive(GameObject go, string sceneName)
    {
        int count = 0;

        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                count++;
                string path = GetPath(go.transform);
                Debug.LogError($"[Missing Script] Scene: {sceneName} / Object: {path}", go);
            }
        }

        foreach (Transform child in go.transform)
        {
            count += ScanGameObjectRecursive(child.gameObject, sceneName);
        }

        return count;
    }

    private static string GetPath(Transform current)
    {
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }
        return path;
    }

    [MenuItem("Tools/Remove Missing Scripts From Selected Objects")]
    public static void RemoveFromSelected()
    {
        int removed = 0;

        foreach (var go in Selection.gameObjects)
        {
            removed += RemoveRecursive(go);
        }

        Debug.Log($"削除した Missing Script 数: {removed}");
    }

    private static int RemoveRecursive(GameObject go)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        foreach (Transform child in go.transform)
        {
            removed += RemoveRecursive(child.gameObject);
        }

        return removed;
    }
}