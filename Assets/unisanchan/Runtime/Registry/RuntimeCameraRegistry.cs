using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneCameraRegistration
{
    [Tooltip("Cameraオブジェクト名とは無関係な識別ID")]
    public string CameraInstanceId;

    [Tooltip("Scene内に存在するCamera")]
    public Camera Camera;
}

public class RuntimeCameraRegistry : MonoBehaviour
{
    private const string CameraAssetRegistryResourcePath =
    "Camera/CameraAssetRegistry";

    private CameraAssetRegistry cameraAssetRegistry;

    [Header("Scene Cameras")]
    [SerializeField]
    private List<SceneCameraRegistration> sceneCameras =
        new List<SceneCameraRegistration>();
    
    [SerializeField]
    [Tooltip("ゲーム開始時に映像を表示するScene内のCamera")]
    private Camera initialCamera;

    [SerializeField]
    [Tooltip(
        "動的生成したCameraを配置する親Transform。" +
        "未設定の場合はCameraRegistryの子に生成します。")]
    private Transform dynamicCameraParent;

    public string ActiveCameraInstanceId
    {
        get;
        private set;
    }

    public string InitialCameraInstanceId
    {
        get;
        private set;
    }

    private readonly List<string> cameraSelectionHistory =
        new List<string>();

    private readonly Dictionary<string, Camera> cameras =
        new Dictionary<string, Camera>(
            StringComparer.Ordinal);

    private readonly Dictionary<string, GameObject>
        dynamicCameraRoots =
            new Dictionary<string, GameObject>(
                StringComparer.Ordinal);
                
    private void Awake()
    {
        LoadCameraAssetRegistry();
        BuildSceneCameraRegistry();
        SelectInitialCamera();
    }

    private void LoadCameraAssetRegistry()
    {
        cameraAssetRegistry =
            Resources.Load<CameraAssetRegistry>(
                CameraAssetRegistryResourcePath);

        if (cameraAssetRegistry == null)
        {
            Debug.LogError(
                "[Camera Registry] CameraAssetRegistryを読み込めません。" +
                " Assets/Resources/Camera/" +
                "CameraAssetRegistry.assetを確認してください。");
        }
    }

    private void SelectInitialCamera()
    {
        if (initialCamera == null)
        {
            Debug.LogWarning(
                "[Camera Registry] Initial Cameraが設定されていません。");
            return;
        }

        foreach (SceneCameraRegistration registration
                in sceneCameras)
        {
            if (registration == null)
                continue;

            if (registration.Camera != initialCamera)
                continue;

            InitialCameraInstanceId =
                NormalizeId(registration.CameraInstanceId);

            SelectCamera(InitialCameraInstanceId);
            return;
        }

        Debug.LogError(
            "[Camera Registry] Initial Cameraが" +
            "Scene Camerasに登録されていません。");
    }

    private void BuildSceneCameraRegistry()
    {
        cameras.Clear();

        foreach (SceneCameraRegistration registration
                 in sceneCameras)
        {
            if (registration == null)
                continue;

            RegisterCamera(
                registration.CameraInstanceId,
                registration.Camera);
        }
    }

    public bool SpawnCamera(
        string cameraAssetId,
        string cameraInstanceId)
    {
        string normalizedAssetId =
            NormalizeId(cameraAssetId);

        string normalizedInstanceId =
            NormalizeId(cameraInstanceId);

        Camera sourceCamera = null;

        if (!string.IsNullOrWhiteSpace(
                ActiveCameraInstanceId))
        {
            sourceCamera =
                FindCamera(ActiveCameraInstanceId);
        }

        if (string.IsNullOrEmpty(normalizedAssetId))
        {
            Debug.LogError(
                "[Camera Registry] Camera Asset IDが空です。");

            return false;
        }

        if (string.IsNullOrEmpty(normalizedInstanceId))
        {
            Debug.LogError(
                "[Camera Registry] Camera Instance IDが空です。");

            return false;
        }

        if (cameras.ContainsKey(normalizedInstanceId))
        {
            Debug.LogError(
                "[Camera Registry] Camera Instance IDが" +
                "既に使用されています。" +
                $" ID: {normalizedInstanceId}");

            return false;
        }

        if (cameraAssetRegistry == null)
        {
            Debug.LogError(
                "[Camera Registry] CameraAssetRegistryが" +
                "読み込まれていません。");

            return false;
        }

        CameraAssetRegistryEntry asset =
            cameraAssetRegistry.FindAsset(
                normalizedAssetId);

        if (asset == null)
        {
            Debug.LogError(
                "[Camera Registry] Camera Assetが見つかりません。" +
                $" ID: {normalizedAssetId}");

            return false;
        }

        if (asset.Prefab == null)
        {
            Debug.LogError(
                "[Camera Registry] Camera AssetにPrefabが" +
                "設定されていません。" +
                $" ID: {normalizedAssetId}");

            return false;
        }

        Transform spawnParent =
            dynamicCameraParent != null
                ? dynamicCameraParent
                : transform;

        GameObject instance =
            Instantiate(
                asset.Prefab,
                spawnParent,
                true);

        instance.name =
            $"CameraInstance_{normalizedInstanceId}";

        Camera[] instanceCameras =
            instance.GetComponentsInChildren<Camera>(true);

        if (instanceCameras.Length != 1)
        {
            Debug.LogError(
                "[Camera Registry] Camera Prefabには" +
                "Cameraコンポーネントが一つだけ必要です。" +
                $" Asset ID: {normalizedAssetId}" +
                $" / Camera数: {instanceCameras.Length}");

            Destroy(instance);
            return false;
        }

        Camera instanceCamera =
            instanceCameras[0];

        if (sourceCamera != null)
        {
            instanceCamera.transform.SetPositionAndRotation(
                sourceCamera.transform.position,
                sourceCamera.transform.rotation);

            instanceCamera.orthographic =
                sourceCamera.orthographic;

            instanceCamera.fieldOfView =
                sourceCamera.fieldOfView;

            instanceCamera.orthographicSize =
                sourceCamera.orthographicSize;
        }

        instanceCamera.enabled = false;

        AudioListener[] listeners =
            instance.GetComponentsInChildren<AudioListener>(true);

        foreach (AudioListener listener in listeners)
        {
            if (listener != null)
                listener.enabled = false;
        }

        if (!RegisterCamera(
                normalizedInstanceId,
                instanceCamera))
        {
            Destroy(instance);
            return false;
        }

        dynamicCameraRoots.Add(
            normalizedInstanceId,
            instance);

        if (SelectCamera(normalizedInstanceId))
            return true;

        dynamicCameraRoots.Remove(
            normalizedInstanceId);

        UnregisterCamera(
            normalizedInstanceId);

        Destroy(instance);

        return false;
    }

    public bool RegisterCamera(
        string cameraInstanceId,
        Camera camera)
    {
        string normalizedId =
            NormalizeId(cameraInstanceId);

        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogError(
                "[Camera Registry] Camera Instance IDが空です。");

            return false;
        }

        if (camera == null)
        {
            Debug.LogError(
                "[Camera Registry] Camera参照がありません。" +
                $" ID: {normalizedId}");

            return false;
        }

        if (cameras.ContainsKey(normalizedId))
        {
            Debug.LogError(
                "[Camera Registry] Camera Instance IDが重複しています。" +
                $" ID: {normalizedId}");

            return false;
        }

        cameras.Add(
            normalizedId,
            camera);

        return true;
    }

    public bool UnregisterCamera(
        string cameraInstanceId)
    {
        string normalizedId =
            NormalizeId(cameraInstanceId);

        if (string.IsNullOrEmpty(normalizedId))
            return false;

        return cameras.Remove(normalizedId);
    }

    public Camera FindCamera(
        string cameraInstanceId)
    {
        string normalizedId =
            NormalizeId(cameraInstanceId);

        if (string.IsNullOrEmpty(normalizedId))
            return null;

        if (!cameras.TryGetValue(
                normalizedId,
                out Camera camera))
        {
            return null;
        }

        if (camera == null)
        {
            cameras.Remove(normalizedId);

            Debug.LogWarning(
                "[Camera Registry] 登録Cameraが既に削除されています。" +
                $" ID: {normalizedId}");

            return null;
        }

        return camera;
    }

    public bool SelectCamera(
        string cameraInstanceId)
    {
        string normalizedId =
            NormalizeId(cameraInstanceId);

        Camera targetCamera =
            FindCamera(normalizedId);

        if (targetCamera == null)
        {
            Debug.LogError(
                "[Camera Registry] 表示するCameraが見つかりません。" +
                $" ID: {cameraInstanceId}");

            return false;
        }

        string previousId =
            NormalizeId(ActiveCameraInstanceId);

        if (!string.IsNullOrEmpty(previousId) &&
            previousId != normalizedId &&
            FindCamera(previousId) != null)
        {
            cameraSelectionHistory.Add(previousId);
        }

        foreach (KeyValuePair<string, Camera> pair
                in cameras)
        {
            Camera registeredCamera =
                pair.Value;

            if (registeredCamera == null)
                continue;

            registeredCamera.enabled =
                registeredCamera == targetCamera;

            AudioListener[] listeners =
                registeredCamera
                    .GetComponentsInChildren
                        <AudioListener>(true);

            foreach (AudioListener listener
                    in listeners)
            {
                if (listener != null)
                    listener.enabled = false;
            }
        }

        AudioListener[] targetListeners =
            targetCamera.GetComponentsInChildren
                <AudioListener>(true);

        if (targetListeners.Length == 0)
        {
            Debug.LogWarning(
                "[Camera Registry] 選択したCameraに" +
                "AudioListenerがありません。" +
                $" ID: {normalizedId}");
        }
        else
        {
            targetListeners[0].enabled = true;

            if (targetListeners.Length > 1)
            {
                Debug.LogWarning(
                    "[Camera Registry] 選択したCameraに" +
                    "AudioListenerが複数あります。" +
                    "先頭の一つだけを有効にしました。" +
                    $" ID: {normalizedId}");
            }
        }

        ActiveCameraInstanceId =
            normalizedId;

        return true;
    }

    public bool DestroyCamera(
        string cameraInstanceId,
        string returnCameraInstanceId = "")
    {
        string normalizedId =
            NormalizeId(cameraInstanceId);

        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogError(
                "[Camera Registry] 破棄するCamera Instance IDが空です。");

            return false;
        }

        if (!dynamicCameraRoots.TryGetValue(
                normalizedId,
                out GameObject cameraRoot) ||
            cameraRoot == null)
        {
            if (FindCamera(normalizedId) == null)
            {
                Debug.LogWarning(
                    "[Camera Registry] DestroyCameraをスキップしました。" +
                    $" Camera Instance ID: {normalizedId}。" +
                    " 対象の動的Cameraは現在存在しません。" +
                    " 再開・JumpによってSpawnCameraを通っていない場合は" +
                    " そのままで正常です。" +
                    " 表示されているCameraを消す意図だった場合は、" +
                    " SpawnCameraの実行経路とInstance IDを確認してください。");

                return true;
            }

            Debug.LogError(
                "[Camera Registry] DestroyCameraを実行できません。" +
                $" Camera Instance ID: {normalizedId}。" +
                " このIDのCameraは登録されていますが、" +
                " DestroyCameraで破棄できる動的Cameraではありません。" +
                " Scene内のCameraなら別のCameraへ切り替えてください。");

            return false;
        }

        Camera targetCamera =
            FindCamera(normalizedId);

        if (targetCamera == null)
        {
            Debug.LogError(
                "[Camera Registry] 破棄するCameraが見つかりません。" +
                $" ID: {normalizedId}");

            return false;
        }

        bool isActiveCamera =
            NormalizeId(ActiveCameraInstanceId) ==
            normalizedId;

        if (isActiveCamera)
        {
            string replacementId =
                ResolveReturnCameraId(
                    normalizedId,
                    returnCameraInstanceId);

            if (string.IsNullOrEmpty(replacementId))
            {
                Debug.LogError(
                    "[Camera Registry] 切り替え先のCameraがないため、" +
                    "使用中のCameraを破棄できません。" +
                    $" ID: {normalizedId}");

                return false;
            }

            if (!SelectCamera(replacementId))
            {
                Debug.LogError(
                    "[Camera Registry] Cameraの切り替えに失敗したため、" +
                    "破棄を中止しました。" +
                    $" ID: {normalizedId}");

                return false;
            }
        }
        else
        {
            targetCamera.enabled = false;

            AudioListener[] listeners =
                targetCamera.GetComponentsInChildren
                    <AudioListener>(true);

            foreach (AudioListener listener
                    in listeners)
            {
                if (listener != null)
                    listener.enabled = false;
            }
        }

        cameras.Remove(normalizedId);
        dynamicCameraRoots.Remove(normalizedId);

        cameraSelectionHistory.RemoveAll(
            id => NormalizeId(id) == normalizedId);

        Destroy(cameraRoot);

        return true;
    }

    private string ResolveReturnCameraId(
        string destroyCameraInstanceId,
        string requestedReturnCameraInstanceId)
    {
        string requestedId =
            NormalizeId(requestedReturnCameraInstanceId);

        if (!string.IsNullOrEmpty(requestedId))
        {
            if (requestedId == destroyCameraInstanceId)
            {
                Debug.LogError(
                    "[Camera Registry] 破棄対象と切り替え先に" +
                    "同じCamera Instance IDが指定されています。" +
                    $" ID: {requestedId}");

                return "";
            }

            if (FindCamera(requestedId) == null)
            {
                Debug.LogError(
                    "[Camera Registry] 破棄前に切り替えるCameraが" +
                    "見つかりません。" +
                    $" ID: {requestedId}");

                return "";
            }

            return requestedId;
        }

        while (cameraSelectionHistory.Count > 0)
        {
            int lastIndex =
                cameraSelectionHistory.Count - 1;

            string historyId =
                NormalizeId(
                    cameraSelectionHistory[lastIndex]);

            cameraSelectionHistory.RemoveAt(lastIndex);

            if (string.IsNullOrEmpty(historyId))
                continue;

            if (historyId == destroyCameraInstanceId)
                continue;

            if (FindCamera(historyId) != null)
                return historyId;
        }

        string initialId =
            NormalizeId(InitialCameraInstanceId);

        if (!string.IsNullOrEmpty(initialId) &&
            initialId != destroyCameraInstanceId &&
            FindCamera(initialId) != null)
        {
            return initialId;
        }

        foreach (KeyValuePair<string, Camera> pair
                in cameras)
        {
            if (pair.Key == destroyCameraInstanceId)
                continue;

            if (pair.Value != null)
                return pair.Key;
        }

        return "";
    }

    private static string NormalizeId(
        string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim();
    }
}