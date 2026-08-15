using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public sealed class MinMaxRangeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxRangeAttribute range = (MinMaxRangeAttribute)attribute;

        if (property.propertyType != SerializedPropertyType.Vector2)
        {
            EditorGUI.LabelField(position, label.text, "Use MinMaxRange with Vector2.");
            return;
        }

        Vector2 value = property.vector2Value;
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.MinMaxSlider(position, label, ref value.x, ref value.y, range.Minimum, range.Maximum);
        property.vector2Value = value;
        EditorGUI.EndProperty();
    }
}
