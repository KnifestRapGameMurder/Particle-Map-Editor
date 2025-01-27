#if UNITY_EDITOR
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    public class FPSCounter : MonoBehaviour
    {
        private float _deltaTime = 0.0f;

        private void Update()
        {
            // Smoothly calculate delta time for FPS
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            // Calculate FPS
            int fps = Mathf.CeilToInt(1.0f / _deltaTime);

            // Display FPS
            GUIStyle style = new GUIStyle
            {
                fontSize = 50,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(10, 10, 100, 20), $"FPS: {fps}", style);
        }
    }
}
#endif