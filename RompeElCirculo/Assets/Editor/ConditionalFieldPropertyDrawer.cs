using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ConditionalFieldoAttribute))]
public class ConditionalFieldPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalFieldoAttribute cond = (ConditionalFieldoAttribute)attribute;
        SerializedProperty conditionProp = FindConditionProperty(property, cond.ConditionFieldName);

        bool enabled = conditionProp != null && conditionProp.propertyType == SerializedPropertyType.Boolean
            ? conditionProp.boolValue
            : true;

        if (cond.Inverse) enabled = !enabled;

        if (enabled)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalFieldoAttribute cond = (ConditionalFieldoAttribute)attribute;
        SerializedProperty conditionProp = FindConditionProperty(property, cond.ConditionFieldName);

        bool enabled = conditionProp != null && conditionProp.propertyType == SerializedPropertyType.Boolean
            ? conditionProp.boolValue
            : true;

        if (cond.Inverse) enabled = !enabled;

        return enabled ? EditorGUI.GetPropertyHeight(property, label, true) : 0;
    }

    private SerializedProperty FindConditionProperty(SerializedProperty property, string conditionFieldName)
    {
        // Soporta objetos anidados y arrays
        string path = property.propertyPath;
        int lastDot = path.LastIndexOf('.');
        string parentPath = lastDot > 0 ? path.Substring(0, lastDot) : "";
        SerializedProperty conditionProp = property.serializedObject.FindProperty(
            string.IsNullOrEmpty(parentPath) ? conditionFieldName : $"{parentPath}.{conditionFieldName}"
        );
        if (conditionProp == null)
            conditionProp = property.serializedObject.FindProperty(conditionFieldName);
        return conditionProp;
    }
}