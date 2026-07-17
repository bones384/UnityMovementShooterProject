#if UNITY_EDITOR

using Unity.Physics.Authoring;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomEditor(typeof(LimitedHingeJoint))]
    public class LimitedHingeEditor : UnityEditor.Editor
    {
        private readonly EditorUtilities.AxisEditor m_AxisEditor = new();
        private readonly JointAngularLimitHandle m_LimitHandle = new();

        protected virtual void OnSceneGUI()
        {
            var limitedHinge = (LimitedHingeJoint)target;

            if (limitedHinge.EditPivots)
                EditorUtilities.EditPivot(limitedHinge.worldFromA, limitedHinge.worldFromB,
                    limitedHinge.AutoSetConnected,
                    ref limitedHinge.PositionLocal, ref limitedHinge.PositionInConnectedEntity, limitedHinge);
            if (limitedHinge.EditAxes)
                m_AxisEditor.Update(limitedHinge.worldFromA, limitedHinge.worldFromB,
                    limitedHinge.AutoSetConnected,
                    limitedHinge.PositionLocal, limitedHinge.PositionInConnectedEntity,
                    ref limitedHinge.HingeAxisLocal, ref limitedHinge.HingeAxisInConnectedEntity,
                    ref limitedHinge.PerpendicularAxisLocal, ref limitedHinge.PerpendicularAxisInConnectedEntity,
                    limitedHinge);
            if (limitedHinge.EditLimits)
                EditorUtilities.EditLimits(limitedHinge.worldFromA, limitedHinge.worldFromB,
                    limitedHinge.PositionLocal,
                    limitedHinge.HingeAxisLocal, limitedHinge.HingeAxisInConnectedEntity,
                    limitedHinge.PerpendicularAxisLocal, limitedHinge.PerpendicularAxisInConnectedEntity,
                    ref limitedHinge.MinAngle, ref limitedHinge.MaxAngle, m_LimitHandle, limitedHinge);
        }

        public override void OnInspectorGUI()
        {
            var limitedHinge = (LimitedHingeJoint)target;

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Editors:");
            limitedHinge.EditPivots = GUILayout.Toggle(limitedHinge.EditPivots, new GUIContent("Pivot"), "Button");
            limitedHinge.EditAxes = GUILayout.Toggle(limitedHinge.EditAxes, new GUIContent("Axis"), "Button");
            limitedHinge.EditLimits = GUILayout.Toggle(limitedHinge.EditLimits, new GUIContent("Limits"), "Button");
            GUILayout.EndHorizontal();
            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
        }
    }
}

#endif