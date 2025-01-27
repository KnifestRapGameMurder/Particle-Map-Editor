#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace PeopleHarvest.Core
{
    public class BaseScriptableObject : ScriptableObject
    {
        [ScriptableObjectId]
        public string Id;
    }

    public class ScriptableObjectIdAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(ScriptableObjectIdAttribute))]
    public class ScriptableObjectIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            if (string.IsNullOrEmpty(property.stringValue))
            {
                property.stringValue = Guid.NewGuid().ToString();
            }
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif