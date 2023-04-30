using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HardSerializeAttribute))]
public class HardSerializePropertyDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        if (Application.isPlaying) { GUI.enabled = false; }
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
