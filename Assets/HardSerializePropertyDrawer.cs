using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(HardSerializeAttribute))]
public class HardSerializePropertyDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        if (Application.isPlaying) { GUI.enabled = false; }
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif
