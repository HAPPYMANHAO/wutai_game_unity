using UnityEngine;

namespace WutaiTactics.Map
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public MapData MapData { get; set; }
        public string SelectedNodeId { get; set; }
        public bool LastBattleWon { get; set; }
        public bool ReturningFromBattle { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
