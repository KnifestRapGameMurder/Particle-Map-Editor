using UnityEngine;
using UnityEditor;
using OdinSerializer;
using System.IO;

namespace Flexus.ParticleMapEditor.Editor
{
    [CustomEditor(typeof(DefaultAsset))]
    public class MapEditorSaveDataEditor : UnityEditor.Editor
    {
        private MapEditorSaveData saveData;
        private Texture2D texturePreview;
        private bool foldoutTexture = true;

        public override void OnInspectorGUI()
        {
            GUI.enabled = true;
            DrawDefaultInspector();

            DefaultAsset defaultAsset = target as DefaultAsset;

            if (defaultAsset == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(defaultAsset);

            if (!assetPath.EndsWith($".{MapEditorSaveManager.EditorFileExtension}"))
                return;

            if (saveData == null)
            {
                LoadSaveData(assetPath);

                if (saveData == null)
                {
                    EditorGUILayout.HelpBox("No map editor data found or failed to load.", MessageType.Warning);
                    return;
                }

                CreateTexturePreview();
            }

            //EditorGUILayout.LabelField("Map Data Viewer", EditorStyles.boldLabel);

            if (saveData.Texture != null)
            {
                //foldoutTexture = EditorGUILayout.Foldout(foldoutTexture, $"Texture Data ({saveData.Texture.Count} entries)");
                if (foldoutTexture && texturePreview != null)
                {
                    EditorGUI.indentLevel++;

                    // Calculate the square rect for the texture
                    float textureSize = Mathf.Min(EditorGUIUtility.currentViewWidth - 40, 256); // Set max size and keep it square
                    Rect textureRect = GUILayoutUtility.GetRect(textureSize, textureSize, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

                    // Draw the square texture without stretching
                    EditorGUI.DrawPreviewTexture(textureRect, texturePreview);

                    EditorGUI.indentLevel--;

                    //if (GUILayout.Button("Refresh Texture Preview"))
                    //{
                    //    CreateTexturePreview();
                    //}
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Texture data is null or empty.", MessageType.Warning);
            }
        }

        private void LoadSaveData(string assetPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(assetPath);
                saveData = OdinSerializer.SerializationUtility.DeserializeValue<MapEditorSaveData>(bytes, DataFormat.Binary);
                //Debug.Log("Map editor data loaded successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load map editor data: {ex.Message}");
            }
        }

        private void CreateTexturePreview()
        {
            if (saveData == null || saveData.Texture == null || saveData.Texture.Count == 0)
            {
                Debug.LogWarning("No texture data available for preview.");
                return;
            }

            // Determine the maximum width and height of the texture
            int width = 0;
            int height = 0;

            foreach (var kvp in saveData.Texture)
            {
                width = Mathf.Max(width, kvp.Key.x);
                height = Mathf.Max(height, kvp.Key.y);
            }

            // Create a new Texture2D with the appropriate size, ensuring minimum size of 1x1
            texturePreview = new Texture2D(width + 1, height + 1, TextureFormat.RGBA32, false);

            // Set all pixels to clear first (default state)
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    texturePreview.SetPixel(x, y, Color.clear);
                }
            }

            // Apply dictionary data as pixels in the texture
            foreach (var kvp in saveData.Texture)
            {
                texturePreview.SetPixel(kvp.Key.x, kvp.Key.y, kvp.Value);
            }

            texturePreview.Apply();

            //Debug.Log($"Texture preview created with size: {width + 1} x {height + 1}");
        }
    }
}