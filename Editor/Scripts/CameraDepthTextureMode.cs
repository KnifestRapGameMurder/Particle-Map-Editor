#if UNITY_EDITOR
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    [RequireComponent(typeof(Camera))]
    public class CameraDepthTextureMode : MonoBehaviour
    {
        [SerializeField] private DepthTextureMode depthTextureMode;

        private void OnValidate()
        {
            SetCameraDepthTextureMode();
        }

        private void Awake()
        {
            SetCameraDepthTextureMode();
        }

        private void SetCameraDepthTextureMode()
        {
            GetComponent<Camera>().depthTextureMode = depthTextureMode;
        }
    }
}
#endif