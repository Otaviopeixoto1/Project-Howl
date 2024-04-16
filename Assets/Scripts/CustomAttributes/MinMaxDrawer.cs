using UnityEngine;
using UnityEditor;
using System.Security.Cryptography;

#if UNITY_EDITOR 
[CustomPropertyDrawer(typeof(MinMaxAttribute))]
public class MinMaxDrawer : PropertyDrawer
{
    const float padding = 0.1f; 
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.Vector2)
        {
            MinMaxAttribute range = attribute as MinMaxAttribute;

            EditorGUI.BeginProperty(position, label, property);

            float minValue = property.vector2Value.x;
            float maxValue = property.vector2Value.y;

            position = EditorGUI.PrefixLabel(position,label);

            position.x -= position.width * padding;
            position.width *= 1+padding;

            Rect minValueRect = new Rect(position.x, position.y, 40f, position.height);
            minValue = EditorGUI.FloatField(minValueRect, minValue);

            Rect sliderRect = new Rect(position.x + 45f, position.y, position.width - 90f, position.height);
            EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, range.min, range.max);

            Rect maxValueRect = new Rect(position.xMax - 40f, position.y, 40f, position.height);
            maxValue = EditorGUI.FloatField(maxValueRect, maxValue);

            property.vector2Value = new Vector2(minValue, maxValue);

            EditorGUI.EndProperty();
        }
    }
}


#endif