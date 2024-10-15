using UnityEngine;
using SFB;
using System.IO;
using OdinSerializer;
using UnityEditor;
using SerializationUtility = OdinSerializer.SerializationUtility;

namespace Flexus.ParticleMapEditor.Editor
{
    public static class DataSaveLoadTools
    {
        private static string _defaultPath => Application.dataPath;

        public static void SaveDataToFile<T>(System.Func<T> getData, string fileExtension, string defaultPath = null)
        {
            var path = StandaloneFileBrowser.SaveFilePanel("Save File", defaultPath ?? _defaultPath, "NewLevel", fileExtension);

            if (string.IsNullOrEmpty(path))
                return;

            byte[] bytes = SerializationUtility.SerializeValue(getData(), DataFormat.Binary);
            File.WriteAllBytes(path, bytes);
            UnityEditor.AssetDatabase.Refresh();
        }

        public static bool TryLoadDataFromFile<T>(string fileExtension, out T data, string defaultPath = null)
        {
            data = default;
            var paths = StandaloneFileBrowser.OpenFilePanel("Open File", defaultPath ?? _defaultPath, fileExtension, false);
            
            if(paths == null || paths.Length == 0) 
                return false;
            
            byte[] bytes = File.ReadAllBytes(paths[0]);
            data = SerializationUtility.DeserializeValue<T>(bytes, DataFormat.Binary);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">Path to folder whithout 'Assets/'</param>
        /// <param name="name">Name of asset</param>
        public static void SaveAsset(string path, string name, Object asset)
        {
            path = $"Assets/{path}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }
}