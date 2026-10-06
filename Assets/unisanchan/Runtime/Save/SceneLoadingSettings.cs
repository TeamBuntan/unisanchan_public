using UnityEngine;

namespace unisanchan
{
    [CreateAssetMenu(
        menuName = "unisanchan/Scene Loading Settings")]
    public class SceneLoadingSettings : ScriptableObject
    {
        [SerializeField]
        private bool useLoadingScene;

        [SerializeField]
        private string loadingScenePath;

        public bool UseLoadingScene => useLoadingScene;
        public string LoadingScenePath => loadingScenePath;
    }
}