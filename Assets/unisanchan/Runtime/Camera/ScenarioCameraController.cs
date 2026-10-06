using System;
using System.Collections;
using UnityEngine;
using unisanchan;

public class ScenarioCameraController : MonoBehaviour
{
    [Header("Camera")]

    [SerializeField]
    private RuntimeCameraRegistry cameraRegistry;

    private Coroutine moveCoroutine;
    private Coroutine fovCoroutine;

    private Action moveCompletionCallback;
    private Action fovCompletionCallback;

    public Vector3 CurrentPosition
    {
        get
        {
            Camera currentCamera =
                ResolveCamera("");

            return currentCamera != null
                ? currentCamera.transform.position
                : Vector3.zero;
        }
    }

    public Quaternion CurrentRotation
    {
        get
        {
            Camera currentCamera =
                ResolveCamera("");

            return currentCamera != null
                ? currentCamera.transform.rotation
                : Quaternion.identity;
        }
    }

    public float CurrentFOV
    {
        get
        {
            Camera currentCamera =
                ResolveCamera("");

            return currentCamera != null
                ? currentCamera.fieldOfView
                : 60f;
        }
    }

    public Vector3 CurrentLocalPosition
    {
        get
        {
            Camera currentCamera =
                ResolveCamera("");

            return currentCamera != null
                ? currentCamera.transform.localPosition
                : Vector3.zero;
        }
    }

    public Quaternion CurrentLocalRotation
    {
        get
        {
            Camera currentCamera =
                ResolveCamera("");

            return currentCamera != null
                ? currentCamera.transform.localRotation
                : Quaternion.identity;
        }
    }

    public Transform CameraTransform
    {
        get
        {
            Camera currentCamera =
                ResolveCamera("");

            return currentCamera != null
                ? currentCamera.transform
                : null;
        }
    }

    public Camera FindCamera(
        string cameraInstanceId)
    {
        if (string.IsNullOrWhiteSpace(
                cameraInstanceId))
        {
            return ResolveCamera("");
        }

        if (cameraRegistry == null)
        {
            Debug.LogWarning(
                "[Camera Controller] RuntimeCameraRegistryが" +
                "設定されていません。");

            return null;
        }

        Camera camera =
            cameraRegistry.FindCamera(
                cameraInstanceId);

        if (camera == null)
        {
            Debug.LogWarning(
                "[Camera Controller] Cameraが見つかりません。" +
                $" ID: {cameraInstanceId}");

            return null;
        }

        return camera;
    }

    public Camera ResolveCamera(
        string cameraInstanceId)
    {
        string normalizedId =
            string.IsNullOrWhiteSpace(cameraInstanceId)
                ? ""
                : cameraInstanceId.Trim();

        if (!string.IsNullOrEmpty(normalizedId))
        {
            return FindCamera(normalizedId);
        }

        if (cameraRegistry != null &&
            !string.IsNullOrWhiteSpace(
                cameraRegistry.ActiveCameraInstanceId))
        {
            Camera activeCamera =
                cameraRegistry.FindCamera(
                    cameraRegistry.ActiveCameraInstanceId);

            if (activeCamera != null)
                return activeCamera;
        }

        Debug.LogWarning(
            "[Camera Controller] 操作対象のCameraを解決できません。");

        return null;
    }

    public Vector3 GetCurrentPosition(
        Camera targetCamera)
    {
        return targetCamera != null
            ? targetCamera.transform.position
            : Vector3.zero;
    }

    public Quaternion GetCurrentRotation(
        Camera targetCamera)
    {
        return targetCamera != null
            ? targetCamera.transform.rotation
            : Quaternion.identity;
    }

    public Vector3 GetCurrentLocalPosition(
        Camera targetCamera)
    {
        return targetCamera != null
            ? targetCamera.transform.localPosition
            : Vector3.zero;
    }

    public Quaternion GetCurrentLocalRotation(
        Camera targetCamera)
    {
        return targetCamera != null
            ? targetCamera.transform.localRotation
            : Quaternion.identity;
    }

    public void MoveToPositionWithDuration(
        Vector3 targetPosition,
        float duration,
        EasingType easingType = EasingType.Linear,
        Action onCompleted = null)
    {
        Camera targetCamera =
            ResolveCamera("");

        if (targetCamera == null)
        {
            onCompleted?.Invoke();
            return;
        }

        StopMoveCoroutine();

        moveCompletionCallback =
            onCompleted;

        moveCoroutine = StartCoroutine(
            MoveCoroutine(
                targetCamera,
                targetPosition,
                targetCamera.transform.rotation,
                duration,
                easingType
            )
        );
    }

    public void MoveToLocalPositionAndRotationWithDuration(
        Vector3 targetLocalPosition,
        Quaternion targetLocalRotation,
        float duration,
        EasingType easingType = EasingType.Linear,
        Action onCompleted = null)
    {
        MoveToLocalPositionAndRotationWithDuration(
            ResolveCamera(""),
            targetLocalPosition,
            targetLocalRotation,
            duration,
            easingType,
            onCompleted
        );
    }

    public void MoveToLocalPositionAndRotationWithDuration(
        Camera targetCamera,
        Vector3 targetLocalPosition,
        Quaternion targetLocalRotation,
        float duration,
        EasingType easingType = EasingType.Linear,
        Action onCompleted = null)
    {
        if (targetCamera == null)
        {
            Debug.LogWarning(
                "[Camera Controller] Local移動対象のCameraがありません。");

            onCompleted?.Invoke();
            return;
        }

        StopMoveCoroutine();

        moveCompletionCallback =
            onCompleted;

        moveCoroutine = StartCoroutine(
            MoveLocalCoroutine(
                targetCamera,
                targetLocalPosition,
                targetLocalRotation,
                duration,
                easingType
            )
        );
    }

    public void MoveToFOVWithDuration(
        float targetFOV,
        float duration,
        EasingType easingType = EasingType.Linear,
        Action onCompleted = null)
    {
        MoveToFOVWithDuration(
            ResolveCamera(""),
            targetFOV,
            duration,
            easingType,
            onCompleted
        );
    }

    public void MoveToFOVWithDuration(
        Camera targetCamera,
        float targetFOV,
        float duration,
        EasingType easingType = EasingType.Linear,
        Action onCompleted = null)
    {
        if (targetCamera == null)
        {
            Debug.LogWarning(
                "[Camera Controller] FOV変更対象のCameraがありません。");

            onCompleted?.Invoke();
            return;
        }

        StopFOVCoroutine();

        fovCompletionCallback =
            onCompleted;

        fovCoroutine = StartCoroutine(
            FOVCoroutine(
                targetCamera,
                targetFOV,
                duration,
                easingType
            )
        );
    }

    private IEnumerator MoveCoroutine(
        Camera targetCamera,
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration,
        EasingType easingType)
    {
        if (targetCamera == null)
        {
            CompleteMoveTransition();
            yield break;
        }

        if (duration <= 0f)
        {
            targetCamera.transform.position =
                targetPosition;

            targetCamera.transform.rotation =
                targetRotation;

            CompleteMoveTransition();
            yield break;
        }

        Vector3 startPosition =
            targetCamera.transform.position;

        Quaternion startRotation =
            targetCamera.transform.rotation;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time / duration);

            float easedProgress =
                EasingUtility.Evaluate(
                    easingType,
                    progress);

            targetCamera.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    easedProgress);

            targetCamera.transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    easedProgress);

            yield return null;
        }

        targetCamera.transform.position =
            targetPosition;

        targetCamera.transform.rotation =
            targetRotation;

        CompleteMoveTransition();
    }

    private IEnumerator MoveLocalCoroutine(
        Camera targetCamera,
        Vector3 targetLocalPosition,
        Quaternion targetLocalRotation,
        float duration,
        EasingType easingType)
    {
        if (targetCamera == null)
        {
            CompleteMoveTransition();
            yield break;
        }

        if (duration <= 0f)
        {
            targetCamera.transform.localPosition =
                targetLocalPosition;

            targetCamera.transform.localRotation =
                targetLocalRotation;

            CompleteMoveTransition();
            yield break;
        }

        Vector3 startPosition =
            targetCamera.transform.localPosition;

        Quaternion startRotation =
            targetCamera.transform.localRotation;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time / duration);

            float easedProgress =
                EasingUtility.Evaluate(
                    easingType,
                    progress);

            targetCamera.transform.localPosition =
                Vector3.Lerp(
                    startPosition,
                    targetLocalPosition,
                    easedProgress);

            targetCamera.transform.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetLocalRotation,
                    easedProgress);

            yield return null;
        }

        targetCamera.transform.localPosition =
            targetLocalPosition;

        targetCamera.transform.localRotation =
            targetLocalRotation;

        CompleteMoveTransition();
    }

    private IEnumerator FOVCoroutine(
        Camera targetCamera,
        float targetFOV,
        float duration,
        EasingType easingType)
    {
        if (targetCamera == null)
        {
            CompleteFOVTransition();
            yield break;
        }

        if (duration <= 0f)
        {
            targetCamera.fieldOfView =
                targetFOV;

            CompleteFOVTransition();
            yield break;
        }

        float startFOV =
            targetCamera.fieldOfView;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time / duration);

            float easedProgress =
                EasingUtility.Evaluate(
                    easingType,
                    progress);

            targetCamera.fieldOfView =
                Mathf.Lerp(
                    startFOV,
                    targetFOV,
                    easedProgress);

            yield return null;
        }

        targetCamera.fieldOfView =
            targetFOV;

        CompleteFOVTransition();
    }

    private void CompleteMoveTransition()
    {
        moveCoroutine = null;

        Action callback =
            moveCompletionCallback;

        moveCompletionCallback = null;

        callback?.Invoke();
    }

    private void CompleteFOVTransition()
    {
        fovCoroutine = null;

        Action callback =
            fovCompletionCallback;

        fovCompletionCallback = null;

        callback?.Invoke();
    }

    private void StopMoveCoroutine()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (moveCompletionCallback != null)
        {
            Action callback =
                moveCompletionCallback;

            moveCompletionCallback = null;

            callback.Invoke();
        }
    }

    private void StopFOVCoroutine()
    {
        if (fovCoroutine != null)
        {
            StopCoroutine(fovCoroutine);
            fovCoroutine = null;
        }

        if (fovCompletionCallback != null)
        {
            Action callback =
                fovCompletionCallback;

            fovCompletionCallback = null;

            callback.Invoke();
        }
    }

    public void MoveToPositionAndRotationWithDuration(
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration,
        EasingType easingType = EasingType.Linear,
        Action onCompleted = null)
    {
        MoveToPositionAndRotationWithDuration(
            ResolveCamera(""),
            targetPosition,
            targetRotation,
            duration,
            easingType,
            onCompleted
        );
    }

    public void MoveToPositionAndRotationWithDuration(
        Camera targetCamera,
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration,
        EasingType easingType = EasingType.Linear,
        Action onCompleted = null)
    {
        if (targetCamera == null)
        {
            Debug.LogWarning(
                "[Camera Controller] 移動対象のCameraがありません。");

            onCompleted?.Invoke();
            return;
        }

        StopMoveCoroutine();

        moveCompletionCallback =
            onCompleted;

        moveCoroutine = StartCoroutine(
            MoveCoroutine(
                targetCamera,
                targetPosition,
                targetRotation,
                duration,
                easingType
            )
        );
    }

    public bool SpawnCamera(
        string cameraAssetId,
        string cameraInstanceId)
    {
        if (cameraRegistry == null)
        {
            Debug.LogWarning(
                "[Camera Controller] RuntimeCameraRegistryが" +
                "設定されていません。");

            return false;
        }

        return cameraRegistry.SpawnCamera(
            cameraAssetId,
            cameraInstanceId);
    }

    public bool DestroyCamera(
        string cameraInstanceId,
        string returnCameraInstanceId = "")
    {
        if (cameraRegistry == null)
        {
            Debug.LogWarning(
                "[Camera Controller] RuntimeCameraRegistryが" +
                "設定されていません。");

            return false;
        }

        return cameraRegistry.DestroyCamera(
            cameraInstanceId,
            returnCameraInstanceId);
    }

    public bool SelectCamera(string cameraInstanceId)
    {
        if (cameraRegistry == null)
        {
            Debug.LogWarning(
                "[Camera Controller] RuntimeCameraRegistryが" +
                "設定されていません。");

            return false;
        }

        return cameraRegistry.SelectCamera(
            cameraInstanceId);
    }
}
