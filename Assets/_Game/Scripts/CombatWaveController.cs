using UnityEngine;

namespace WutaiTactics
{
    public class CombatWaveController : MonoBehaviour
    {
        public static CombatWaveController Instance { get; private set; }

        [Header("Wave Settings")]
        [Tooltip("开场时场上至少有多少敌人")]
        public int initialSpawnCount = 4;
        [Tooltip("玩家行动后若场上存活敌人低于此值，则补一波")]
        public int minAliveEnemies = 4;
        [Tooltip("场上同时存活敌人上限")]
        public int maxAliveEnemies = 9;
        [Tooltip("每波补充多少个敌人")]
        public int spawnPerWave = 3;
        [Tooltip("本场战斗需要击杀多少敌人才算胜利")]
        public int totalKillThreshold = 12;

        public int TotalSpawned { get; private set; }
        public int KillCount { get; private set; }
        public bool SpawningComplete { get; private set; }

        private BattleInitializer spawner;

        private void Awake()
        {
            Instance = this;
            spawner = FindAnyObjectByType<BattleInitializer>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BeginCombat()
        {
            if (spawner == null)
            {
                Debug.LogError("[CombatWaveController] 找不到 BattleInitializer，无法生成敌人。");
                return;
            }

            SpawnWave(initialSpawnCount);
        }

        /// <summary>
        /// 在合适的时机调用，保持场上敌人数量始终多于玩家角色。
        /// </summary>
        public void TryReplenish()
        {
            if (spawner == null || CombatManager.Instance == null)
                return;

            int alive = CombatManager.Instance.enemies.Count;

            while (alive < minAliveEnemies && TotalSpawned < totalKillThreshold)
            {
                int remaining = totalKillThreshold - TotalSpawned;
                int room = maxAliveEnemies - alive;
                int toSpawn = Mathf.Min(spawnPerWave, remaining, room);

                if (toSpawn <= 0)
                    break;

                int spawned = SpawnWave(toSpawn);
                if (spawned == 0)
                    break; // 没空位了，等玩家清出空间

                alive = CombatManager.Instance.enemies.Count;
            }

            if (TotalSpawned >= totalKillThreshold)
                SpawningComplete = true;
        }

        public void RegisterKill()
        {
            KillCount++;
        }

        public bool IsFinalWaveCleared()
        {
            return SpawningComplete && CombatManager.Instance != null && CombatManager.Instance.enemies.Count == 0;
        }

        private int SpawnWave(int count)
        {
            int spawned = spawner.SpawnEnemyWave(count);
            TotalSpawned += spawned;
            if (TotalSpawned >= totalKillThreshold)
                SpawningComplete = true;
            return spawned;
        }
    }
}
