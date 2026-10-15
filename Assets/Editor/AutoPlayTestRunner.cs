using System.Collections;
using UnityEditor;
using UnityEngine;

namespace WutaiTactics.Editor
{
    public static class AutoPlayTestRunner
    {
        [MenuItem("WutaiTactics/Run AutoBattleTest")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[AutoPlayTestRunner] Entering Play Mode for AutoBattleTest...");
                EditorApplication.isPlaying = true;
            }
            else
            {
                Debug.Log("[AutoPlayTestRunner] Already in Play Mode.");
            }
        }
    }
}
