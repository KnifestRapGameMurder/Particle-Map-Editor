using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TriInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexus.ParticleMapEditor.Editor
{
    public static class EditorUtils
    {
        /// <summary>
        /// For Dropdown can NOT be called directly. Use local field of function to wrap.
        /// </summary>
        private static IEnumerable<TriDropdownItem<ScriptableObject>> GetAllConfigAssets(
            Func<ScriptableObject, bool> filterPredicate)
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}", null);
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath);
            var assets = paths.Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>).Where(filterPredicate);
            var scriptableObjects = assets as ScriptableObject[] ?? assets.ToArray();

            if (!scriptableObjects.Any())
            {
                return new TriDropdownList<ScriptableObject>
                {
                    { "NO CONFIGS FOUND", null }
                };
            }

            return scriptableObjects.Select(so => new TriDropdownItem<ScriptableObject> { Text = so.name, Value = so });
        }

        /// <summary>
        /// For Dropdown can NOT be called directly. Use local field of function to wrap.
        /// </summary>
        public static IEnumerable<TriDropdownItem<ScriptableObject>> GetAllResourceConfigAssets()
        {
            return GetAllConfigAssets(so => so is IResourceConfig);
        }

        /// <summary>
        /// For Dropdown can NOT be called directly. Use local field of function to wrap.
        /// </summary>
        public static IEnumerable<TriDropdownItem<ScriptableObject>> GetAllMapConfigAssets()
        {
            return GetAllConfigAssets(so => so is IMapConfig);
        }

        public static void SaveAsset(string path, string name, Object asset)
        {
            // Build the complete path to the file
            string fullPath = $"Assets/{path}";

            // Check if the directory exists, and if not, create it
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Build the full path to the asset file
            string assetPath = $"{fullPath}/{name}.asset";

            // Save the asset at the given path
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Focus on the newly created asset in the project window
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        public static void OpenScriptableObjectInInspector(ScriptableObject scriptableObject)
        {
            if (scriptableObject != null)
            {
                // Focus on the Project window to ensure the Inspector updates
                EditorUtility.FocusProjectWindow();

                // Select the scriptable object in the editor
                Selection.activeObject = scriptableObject;

                // Optionally ping the object to highlight it in the project hierarchy
                EditorGUIUtility.PingObject(scriptableObject);
            }
            else
            {
                Debug.LogError("ScriptableObject is null and cannot be opened in the inspector.");
            }
        }
    }
}