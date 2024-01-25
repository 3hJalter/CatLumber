﻿using UnityEditor;
using UnityEngine;

namespace ToonyColorsPro
{
    public class Tooltip : EditorWindow
    {
        private const float closeDelay = 0.1f;
        private static bool assemblyReload;
        private static Tooltip instance;
        private static readonly GUIContent guiContent = new();
        private static float closeTime;
        private static bool updateEvent;
        private static bool isHiding;
        private static Rect _labelRect;

        private static GUIStyle _style;

        private static GUIStyle style
        {
            get
            {
                if (_style == null)
                {
                    _style = new GUIStyle(EditorStyles.wordWrappedLabel);
                    _style.richText = true;
                    _style.alignment = TextAnchor.MiddleLeft;
                }

                return _style;
            }
        }

        private void OnGUI()
        {
            // draw background
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(1, 1, 1, 0.1f));

            // draw border
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 1), Color.black);
            EditorGUI.DrawRect(new Rect(0, 0, 1, position.height), Color.black);
            EditorGUI.DrawRect(new Rect(position.width - 1, 0, 1, position.height), Color.black);
            EditorGUI.DrawRect(new Rect(0, position.height - 1, position.width, 1), Color.black);

            // label
            GUI.Label(_labelRect, guiContent, style);
        }

        public static void Show(Vector2 position, string message)
        {
            Show(position, 250, message);
        }

        public static void Show(Vector2 position, float width, string message)
        {
            if (instance == null)
            {
                Tooltip[] windows = Resources.FindObjectsOfTypeAll<Tooltip>();

                if (windows.Length > 0)
                {
                    // destroy any lingering window
                    for (int i = 1; i < windows.Length; i++)
                    {
                        windows[i].Close();
                        DestroyImmediate(windows[i]);
                    }

                    instance = windows[0];
                }
                else
                {
                    instance = CreateInstance<Tooltip>();
                    instance.minSize = Vector2.zero;
                }
            }


            const float padding = 4.0f;

            guiContent.text = message.Replace("  ", "\n");
            float height = style.CalcHeight(guiContent, width) + padding;
            instance.position = new Rect(position.x, position.y, width + padding, height);
            _labelRect.x = padding / 2.0f;
            _labelRect.width = width;
            _labelRect.height = instance.position.height;
            instance.ShowPopup();
            isHiding = false;
        }

        public static void Hide()
        {
            if (!isHiding && instance != null)
            {
                isHiding = true;
                closeTime = Time.realtimeSinceStartup + closeDelay;

                if (!updateEvent)
                {
                    EditorApplication.update += applicationUpdate;
                    updateEvent = true;
                }
            }
        }

        private static void applicationUpdate()
        {
            if (Time.realtimeSinceStartup > closeTime)
            {
                instance.Close();

                EditorApplication.update -= applicationUpdate;
                updateEvent = false;
            }
        }
    }
}
