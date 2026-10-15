using UnityEngine;

namespace WutaiTactics
{
    public class BattleInitializer : MonoBehaviour
    {
        [Header("References")]
        public BattleGrid battleGrid;
        public CombatManager combatManager;

        [Header("Player Colors")]
        public Color meleePlayerColor = new Color(0.2f, 0.4f, 1f, 1f);
        public Color polearmPlayerColor = new Color(0.2f, 0.8f, 0.8f, 1f);
        public Color arrowPlayerColor = new Color(0.8f, 0.2f, 0.8f, 1f);

        [Header("Enemy Colors")]
        public Color meleeEnemyColor = new Color(1f, 0.2f, 0.2f, 1f);
        public Color arrowEnemyColor = new Color(1f, 0.4f, 0.2f, 1f);

        private void Start()
        {
            if (battleGrid == null)
                battleGrid = FindAnyObjectByType<BattleGrid>();
            if (combatManager == null)
                combatManager = FindAnyObjectByType<CombatManager>();

            if (battleGrid == null || combatManager == null)
            {
                Debug.LogError("[BattleInitializer] BattleGrid or CombatManager not found in scene.");
                return;
            }

            combatManager.battleGrid = battleGrid;

            SpawnPlayers();

            var waveController = FindAnyObjectByType<CombatWaveController>();
            if (waveController != null)
                waveController.BeginCombat();
            else
                SpawnDefaultEnemies();
        }

        private void SpawnPlayers()
        {
            CreatePlayer(CombatRole.Melee, 0, 15, meleePlayerColor, SkillType.Push);
            CreatePlayer(CombatRole.Polearm, 1, 8, polearmPlayerColor, SkillType.Pull);
            CreatePlayer(CombatRole.Arrow, 2, 10, arrowPlayerColor, SkillType.Root);
        }

        private void CreatePlayer(CombatRole role, int row, int atk, Color color, SkillType skill)
        {
            var go = new GameObject($"Player_{role}_{row}");
            go.transform.SetParent(battleGrid.transform);
            go.transform.position = battleGrid.GetPlayerStandPosition(row);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteUtil.GetWhiteSprite();
            sr.color = color;
            sr.sortingOrder = 10;

            var player = go.AddComponent<PlayerCharacter>();
            player.role = role;
            player.attackPower = atk;
            player.rowIndex = row;
            player.maxHealth = 100;
            player.currentHealth = 100;
            player.skillType = skill;
            player.skillCooldownMax = 3;
            player.currentSkillCooldown = 0;

            combatManager.RegisterPlayer(player);
        }

        private void SpawnDefaultEnemies()
        {
            CreateEnemy(CombatRole.Melee, 0, 5, 40, 10, 1, meleeEnemyColor);
            CreateEnemy(CombatRole.Arrow, 2, 5, 30, 10, 1, arrowEnemyColor);
        }

        public int SpawnEnemyWave(int count)
        {
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                var profile = RollEnemyProfile();
                int row = Random.Range(0, battleGrid.rows);
                int col = FindEmptyBackColumn(row);

                if (col < 0)
                {
                    // 该排满了，尝试任意排
                    for (int r = 0; r < battleGrid.rows; r++)
                    {
                        col = FindEmptyBackColumn(r);
                        if (col >= 0)
                        {
                            row = r;
                            break;
                        }
                    }
                }

                if (col < 0)
                    break; // 全图已满

                CreateEnemy(profile.role, row, col, profile.hp, profile.attack, profile.speed, GetEnemyColor(profile.role), profile.isGroupAttacker);
                spawned++;
            }

            if (spawned > 0)
                Debug.Log($"[BattleInitializer] 刷新了 {spawned} 名敌人，当前场上 {combatManager.enemies.Count} 名。");

            return spawned;
        }

        private int FindEmptyBackColumn(int row)
        {
            // 优先从右侧（后排）往前找空位
            for (int col = battleGrid.columns - 1; col >= 0; col--)
            {
                if (battleGrid.GetOccupant(col, row) == null)
                    return col;
            }
            return -1;
        }

        private (CombatRole role, int hp, int attack, int speed, bool isGroupAttacker) RollEnemyProfile()
        {
            int roll = Random.Range(0, 6);
            return roll switch
            {
                0 => (CombatRole.Melee, 40, 10, 1, false),   // 近战标准
                1 => (CombatRole.Melee, 25, 12, 2, false),   // 近战高速
                2 => (CombatRole.Polearm, 20, 8, 1, false),  // 长柄标准
                3 => (CombatRole.Polearm, 20, 0, 1, false),  // 长柄辅助（暂无光环，仅当肉盾）
                4 => (CombatRole.Arrow, 30, 10, 1, false),  // 矢石标准
                5 => (CombatRole.Arrow, 20, 4, 1, true),     // 矢石群攻
                _ => (CombatRole.Melee, 40, 10, 1, false)
            };
        }

        private Color GetEnemyColor(CombatRole role)
        {
            return role switch
            {
                CombatRole.Melee => meleeEnemyColor,
                CombatRole.Polearm => new Color(0.9f, 0.8f, 0.2f, 1f), // 黄色给长柄敌人
                CombatRole.Arrow => arrowEnemyColor,
                _ => Color.gray
            };
        }

        public void CreateEnemy(CombatRole role, int row, int col, int hp, int atk, int speed, Color color, bool isGroupAttacker = false)
        {
            var go = new GameObject($"Enemy_{role}_{row}_{col}");
            go.transform.SetParent(battleGrid.transform);
            go.transform.position = battleGrid.GetCellWorldPosition(col, row);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteUtil.GetWhiteSprite();
            sr.color = color;
            sr.sortingOrder = 10;

            // 群攻敌人稍微小一点，便于区分
            if (isGroupAttacker)
                sr.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var enemy = go.AddComponent<EnemyCharacter>();
            enemy.role = role;
            enemy.maxHealth = hp;
            enemy.currentHealth = hp;
            enemy.attackPower = atk;
            enemy.moveSpeed = speed;
            enemy.rowIndex = row;
            enemy.columnIndex = col;

            battleGrid.SetOccupant(col, row, enemy);
            combatManager.RegisterEnemy(enemy);
        }
    }
}
