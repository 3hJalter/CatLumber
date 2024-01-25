using UnityEditor;
using UnityEngine;

namespace VinhLB
{
    public class GameUtilityWindow : EditorWindow
    {
        [MenuItem("Tools/VinhLB/Game Utility Panel")]
        public static void ShowWindow()
        {
            GetWindow<GameUtilityWindow>(true, "Game Utility Panel");
        }

        private void OnGUI()
        {
            GUILayout.Label("Daily Reward", EditorStyles.boldLabel);
            if (GUILayout.Button("Print Parameters"))
            {
                DailyRewardManager.PrintParameters();
            }
            if (GUILayout.Button("Set Can Collect Today"))
            {
                DailyRewardManager.SetCanCollectToday();
            }
            if (GUILayout.Button("Increase 1 Daily Day"))
            {
                DailyRewardManager.Increase1DailyDay();
            }
            if (GUILayout.Button("Decrease 1 Daily Day"))
            {
                DailyRewardManager.Decrease1DailyDay();
            }
            if (GUILayout.Button("Reset All"))
            {
                DailyRewardManager.ResetAll();
            }
            
            GUILayout.Label("Level", EditorStyles.boldLabel);
            if (GUILayout.Button("Set Level To 3"))
            {
                PlayerPrefs.SetInt(Constants.LEVEL_INDEX, 3);
            }
        }
    }
}