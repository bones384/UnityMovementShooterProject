using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    internal static class SceneViewUtility
    {
        private const string k_NotificationsPrefKey = "SceneView/Tools/Notifications";
        private const bool k_DefaultNotifications = true;
        private const string k_NotificationSpeedPrefKey = "SceneView/Tools/Notification Speed";
        private const float k_DefaultNotificationsSpeed = 20f;

        private const float k_NotificationDuration = 1f;
        private const float k_NotificationFadeInTime = 0.04f;
        private const float k_NotificationFadeOutTime = 0.2f;

        private const float k_IndeterminateProgressCurveDuration = 2f;

        private static readonly AnimationCurve k_NotificationFadeCurve = new()
        {
            keys = new[]
            {
                new Keyframe { time = 0f, value = 0f, outTangent = 1f / k_NotificationFadeInTime },
                new Keyframe { time = k_NotificationFadeInTime, value = 1f, inTangent = 0f, outTangent = 0f },
                new Keyframe
                {
                    time = k_NotificationDuration - k_NotificationFadeOutTime, value = 1f, inTangent = 0f,
                    outTangent = 0f
                },
                new Keyframe { time = k_NotificationDuration, value = 0f, inTangent = -1f / k_NotificationFadeOutTime }
            },
            postWrapMode = WrapMode.Clamp,
            preWrapMode = WrapMode.Clamp
        };

        private static readonly AnimationCurve k_IndeterminateProgressCurveLeftMargin = new()
        {
            keys = new[]
            {
                new Keyframe { time = 0f, value = 0f, inTangent = 0f, outTangent = 0f },
                new Keyframe
                {
                    time = k_IndeterminateProgressCurveDuration / 2f, value = 0.25f, inTangent = 0f, outTangent = 0f
                },
                new Keyframe
                    { time = k_IndeterminateProgressCurveDuration, value = 1f, inTangent = 0f, outTangent = 0f }
            },
            postWrapMode = WrapMode.Loop,
            preWrapMode = WrapMode.Loop
        };

        private static readonly AnimationCurve k_IndeterminateProgressCurveRightMargin = new()
        {
            keys = new[]
            {
                new Keyframe { time = 0f, value = 1f, inTangent = 0f, outTangent = 0f },
                new Keyframe
                    { time = k_IndeterminateProgressCurveDuration / 2f, value = 0f, inTangent = 0f, outTangent = 0f },
                new Keyframe
                    { time = k_IndeterminateProgressCurveDuration, value = 0f, inTangent = 0f, outTangent = 0f }
            },
            postWrapMode = WrapMode.Loop,
            preWrapMode = WrapMode.Loop
        };

        private static string s_StatusMessage;
        private static DateTime s_StartTime;
        private static bool s_IsTemporary;
        private static Func<float> s_GetProgress;

        public static void DisplayProgressNotification(string message, Func<float> getProgress)
        {
            // insert an extra line to make room for progress bar
            DisplayNotificationInSceneView(getProgress == null ? message : $"{message}\n", false, getProgress);
        }

        public static void DisplayPersistentNotification(string message)
        {
            DisplayNotificationInSceneView(message, false, null);
        }

        public static void DisplayTemporaryNotification(string message)
        {
            DisplayNotificationInSceneView(message, true, null);
        }

        private static void DisplayNotificationInSceneView(string message, bool temporary, Func<float> getProgress)
        {
            s_StatusMessage = message ?? string.Empty;
            s_StartTime = DateTime.Now;
            s_IsTemporary = temporary;
            s_GetProgress = getProgress;
            ClearNotificationInSceneView();
            SceneView.duringSceneGui += ToolNotificationCallback;
            SceneView.RepaintAll();
        }

        private static void ToolNotificationCallback(SceneView obj)
        {
            if (Camera.current == null)
                return;

            var duration = math.max(s_StatusMessage.Length, 1)
                           / EditorPrefs.GetFloat(k_NotificationSpeedPrefKey, k_DefaultNotificationsSpeed);
            var t = (float)(DateTime.Now - s_StartTime).TotalSeconds;
            if (
                s_IsTemporary
                && (t >= duration || !EditorPrefs.GetBool(k_NotificationsPrefKey, k_DefaultNotifications))
            )
            {
                ClearNotificationInSceneView();
            }
            else
            {
                Handles.BeginGUI();
                var color = GUI.color;
                var progress = s_GetProgress?.Invoke() ?? 0f;
                GUI.color *=
                    new Color(1f, 1f, 1f, math.max(k_NotificationFadeCurve.Evaluate(math.abs(t) / duration), progress));
                var rect = new Rect { size = Camera.current.pixelRect.size / EditorGUIUtility.pixelsPerPoint };
                using (new GUILayout.AreaScope(rect))
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Space(rect.height * 0.75f);
                        GUILayout.FlexibleSpace();
                        var maxWidth = rect.width * 0.5f;
                        GUILayout.Box(s_StatusMessage, Styles.SceneViewStatusMessage, GUILayout.MaxWidth(maxWidth));
                        if (s_GetProgress != null)
                        {
                            rect = GUILayoutUtility.GetLastRect();
                            rect = Styles.SceneViewStatusMessage.padding.Remove(rect);
                            rect.y = rect.yMax - Styles.ProgressBarTrack.fixedHeight;
                            rect.height = Styles.ProgressBarTrack.fixedHeight;
                            var c = GUI.color;
                            GUI.color *= Color.black;
                            GUI.Box(rect, GUIContent.none, Styles.ProgressBarTrack);
                            GUI.color = c;
                            if (progress >= 0f && progress <= 1f)
                            {
                                rect.width *= progress;
                            }
                            else
                            {
                                var w = rect.width;
                                rect.xMin = rect.xMin + w * k_IndeterminateProgressCurveLeftMargin.Evaluate(t);
                                rect.xMax = rect.xMax - w * k_IndeterminateProgressCurveRightMargin.Evaluate(t);
                            }

                            GUI.Box(rect, GUIContent.none, Styles.ProgressBarIndicator);
                        }

                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();
                }

                GUI.color = color;
                Handles.EndGUI();
            }

            SceneView.RepaintAll();
        }

        public static void ClearNotificationInSceneView()
        {
            SceneView.duringSceneGui -= ToolNotificationCallback;
        }

        private static class Styles
        {
            public static readonly GUIStyle ProgressBarTrack = new()
            {
                fixedHeight = 4f,
                normal = new GUIStyleState { background = Texture2D.whiteTexture }
            };

            public static readonly GUIStyle ProgressBarIndicator = new()
            {
                fixedHeight = 4f,
                normal = new GUIStyleState { background = Texture2D.whiteTexture }
            };

            public static readonly GUIStyle SceneViewStatusMessage = new("NotificationBackground")
            {
                fontSize = EditorStyles.label.fontSize
            };

            static Styles()
            {
                SceneViewStatusMessage.padding = SceneViewStatusMessage.border;
            }
        }
    }
}