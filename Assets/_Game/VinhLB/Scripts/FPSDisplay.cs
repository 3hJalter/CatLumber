using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VinhLB
{
    public class FPSDisplay : MonoBehaviour
    {
        [System.Serializable]
        [InlineProperty]
        public struct FpsProps
        {
            [HorizontalGroup]
            [HideLabel]
            public int Threshold;
            [HorizontalGroup]
            [HideLabel]
            public Color TextColor;
        }

        [SerializeField]
        private FpsProps _goodFpsProps = default;
        [SerializeField]
        private FpsProps _cautionFpsProps = default;
        [SerializeField]
        private FpsProps _criticalFpsProps = default;
        [SerializeField]
        private int _fontSize = 12;
        [SerializeField]
        private float _updatePeriod = 0.5f;

        private float _fpsAvg = 0f;
        private float _msecsAvg = 0f;

        private float _lastUpdated = 0f;

        private void Update()
        {
            if (Time.time - _lastUpdated > _updatePeriod)
            {
                float fps = 1.0f / Time.unscaledDeltaTime;
                _fpsAvg = (_fpsAvg + fps) / 2;

                float msecs = Time.unscaledDeltaTime * 1000.0f;
                _msecsAvg = (_msecsAvg + msecs) / 2;
                _lastUpdated = Time.time;
            }
        }

        private void OnGUI()
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            Rect rect = new Rect(8, 8, screenWidth, screenHeight);

            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = _fontSize;

            Color textColor = _goodFpsProps.TextColor;
            if (_fpsAvg < _cautionFpsProps.Threshold)
            {
                textColor = _criticalFpsProps.TextColor;
            }
            else if (_fpsAvg < _goodFpsProps.Threshold)
            {
                textColor = _cautionFpsProps.TextColor;
            }

            style.normal.textColor = textColor;

            string text = $"{_fpsAvg:0.} FPS ({_msecsAvg:0.0}ms)";

            GUI.Label(rect, text, style);
        }
    }
}