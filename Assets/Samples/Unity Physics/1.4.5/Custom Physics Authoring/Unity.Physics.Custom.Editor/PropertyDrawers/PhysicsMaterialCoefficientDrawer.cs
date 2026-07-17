using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsMaterialCoefficient))]
    internal class PhysicsMaterialCoefficientDrawer : BaseDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        protected override bool IsCompatible(SerializedProperty property)
        {
            return true;
        }

        protected override void DoGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(
                new Rect(position) { xMax = position.xMax - Styles.PopupWidth },
                property.FindPropertyRelative("Value"),
                label
            );

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(
                new Rect(position)
                    { xMin = position.xMax - Styles.PopupWidth + EditorGUIUtility.standardVerticalSpacing },
                property.FindPropertyRelative("CombineMode"),
                GUIContent.none
            );
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static class Styles
        {
            public const float PopupWidth = 100f;
        }
    }
}