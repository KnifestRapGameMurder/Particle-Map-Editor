using UnityEngine;
using UnityEditor;

namespace Flexus.ParticleMapEditor.Editor
{
    [CustomEditor(typeof(MapEditorConfig))]
    public class MapEditorConfigEditor : UnityEditor.Editor
    {
        private Texture2D texturePreview;
        private int textureSize = 256; // Adjust as needed

        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            GUI.enabled = true;

            Flexus.ParticleMapEditor.Editor.MapEditorConfig config = (Flexus.ParticleMapEditor.Editor.MapEditorConfig)target;

            if (config.Texture == null || config.Texture.Count == 0)
            {
                GUILayout.Label("No Texture Data");
                return;
            }

            // Create or update texture preview
            if (texturePreview == null || texturePreview.width != textureSize || texturePreview.height != textureSize)
            {
                texturePreview = new Texture2D(textureSize, textureSize);
            }

            // Set pixels based on the texture data in config
            UpdateTexturePreview(config);

            // Draw the texture in the inspector
            GUILayout.Label("Texture Preview:");
            GUILayout.Box(texturePreview, GUILayout.Width(textureSize), GUILayout.Height(textureSize));
        }

        private void UpdateTexturePreview(Flexus.ParticleMapEditor.Editor.MapEditorConfig config)
        {
            // Clear texture (setting it to a default color, e.g., black)
            Color[] colors = new Color[textureSize * textureSize];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black; // Default background color
            }

            // Update texture based on the pixel positions and colors from the config
            foreach (var pixel in config.Texture)
            {
                int x = pixel.Position.x;
                int y = pixel.Position.y;

                // Ensure the pixel is within bounds of the texture
                if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
                {
                    texturePreview.SetPixel(x, y, pixel.Color);
                }
            }

            // Apply the changes to the texture
            texturePreview.Apply();
        }
    }

}