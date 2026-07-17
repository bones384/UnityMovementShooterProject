#if UNITY_EDITOR

using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomEditor(typeof(RagdollJoint))]
    public class RagdollJointEditor : UnityEditor.Editor
    {
        private readonly EditorUtilities.AxisEditor m_AxisEditor = new();
        private readonly JointAngularLimitHandle m_LimitHandle = new();

        protected virtual void OnSceneGUI()
        {
            var ragdoll = (RagdollJoint)target;

            var drawCones = false;
            if (ragdoll.EditPivots)
                EditorUtilities.EditPivot(ragdoll.worldFromA, ragdoll.worldFromB, ragdoll.AutoSetConnected,
                    ref ragdoll.PositionLocal, ref ragdoll.PositionInConnectedEntity, ragdoll);
            if (ragdoll.EditAxes)
            {
                m_AxisEditor.Update(ragdoll.worldFromA, ragdoll.worldFromB, ragdoll.AutoSetConnected,
                    ragdoll.PositionLocal, ragdoll.PositionInConnectedEntity, ref ragdoll.TwistAxisLocal,
                    ref ragdoll.TwistAxisInConnectedEntity,
                    ref ragdoll.PerpendicularAxisLocal, ref ragdoll.PerpendicularAxisInConnectedEntity, ragdoll);
                drawCones = true;
            }

            if (ragdoll.EditLimits)
                EditorUtilities.EditLimits(ragdoll.worldFromA, ragdoll.worldFromB, ragdoll.PositionLocal,
                    ragdoll.TwistAxisLocal, ragdoll.TwistAxisInConnectedEntity,
                    ragdoll.PerpendicularAxisLocal, ragdoll.PerpendicularAxisInConnectedEntity,
                    ref ragdoll.MinTwistAngle, ref ragdoll.MaxTwistAngle, m_LimitHandle, ragdoll);

            if (drawCones)
            {
                var pivotB = math.transform(ragdoll.worldFromB, ragdoll.PositionInConnectedEntity);
                var axisB = math.rotate(ragdoll.worldFromB, ragdoll.TwistAxisInConnectedEntity);
                DrawCone(pivotB, axisB, math.radians(ragdoll.MaxConeAngle), Color.yellow);

                var perpendicularB = math.rotate(ragdoll.worldFromB, ragdoll.PerpendicularAxisInConnectedEntity);
                DrawCone(pivotB, perpendicularB, math.radians(ragdoll.MinPerpendicularAngle + 90f), Color.red);
                DrawCone(pivotB, perpendicularB, math.radians(ragdoll.MaxPerpendicularAngle + 90f), Color.red);
            }
        }

        public override void OnInspectorGUI()
        {
            var ragdoll = (RagdollJoint)target;

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginVertical();
            GUILayout.Space(10.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Editors:");
            ragdoll.EditPivots = GUILayout.Toggle(ragdoll.EditPivots, new GUIContent("Pivot"), "Button");
            ragdoll.EditAxes = GUILayout.Toggle(ragdoll.EditAxes, new GUIContent("Axis"), "Button");
            ragdoll.EditLimits = GUILayout.Toggle(ragdoll.EditLimits, new GUIContent("Limits"), "Button");
            GUILayout.EndHorizontal();

            GUILayout.Space(10.0f);
            GUILayout.EndVertical();

            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
        }

        private static void DrawCone(float3 point, float3 axis, float angle, Color color)
        {
#if UNITY_EDITOR
            Handles.color = color;

            float3 dir;
            var scale = Math.NormalizeWithLength(axis, out dir);

            float3 arm;
            {
                float3 perp1, perp2;
                Math.CalculatePerpendicularNormalized(dir, out perp1, out perp2);
                arm = math.mul(quaternion.AxisAngle(perp1, angle), dir) * scale;
            }

            const int res = 16;
            var q = quaternion.AxisAngle(dir, 2.0f * math.PI / res);
            for (var i = 0; i < res; i++)
            {
                var nextArm = math.mul(q, arm);
                Handles.DrawLine(point, point + arm);
                Handles.DrawLine(point + arm, point + nextArm);
                arm = nextArm;
            }
#endif
        }
    }
}

#endif