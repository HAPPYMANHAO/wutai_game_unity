using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutaiTactics
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Shared HP")]
        public int sharedMaxHealth = 100;
        public int sharedCurrentHealth = 100;

        [Header("State")]
        public BattlePhase currentPhase = BattlePhase.Player;
        public int currentPlayerIndex = 0; // which player is acting now
        public int roundNumber = 1;

        [Header("Result")]
        public bool BattleEnded = false;
        public bool BattleWon = false;

        [Header("References")]
        public BattleGrid battleGrid;
        public List<PlayerCharacter> players = new List<PlayerCharacter>();
        public List<EnemyCharacter> enemies = new List<EnemyCharacter>();

        public HashSet<PlayerCharacter> actedPlayers = new HashSet<PlayerCharacter>();

        public bool IsPlayerPhase => currentPhase == BattlePhase.Player;
        public bool IsEnemyPhase => currentPhase == BattlePhase.Enemy;

        private void Awake()
        {
            Instance = this;
            sharedCurrentHealth = sharedMaxHealth;

            if (battleGrid == null)
                battleGrid = FindAnyObjectByType<BattleGrid>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RegisterPlayer(PlayerCharacter player)
        {
            if (!players.Contains(player))
                players.Add(player);
        }

        public void RegisterEnemy(EnemyCharacter enemy)
        {
            if (!enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public void RemoveEnemy(EnemyCharacter enemy)
        {
            if (enemies.Contains(enemy))
            {
                enemies.Remove(enemy);
                battleGrid.ClearOccupant(enemy.columnIndex, enemy.rowIndex);
                CombatWaveController.Instance?.RegisterKill();
            }
        }

        public PlayerCharacter GetCurrentPlayer()
        {
            if (currentPlayerIndex >= 0 && currentPlayerIndex < players.Count)
                return players[currentPlayerIndex];
            return null;
        }

        public void SetCurrentPlayer(PlayerCharacter player)
        {
            int idx = players.IndexOf(player);
            if (idx >= 0)
                currentPlayerIndex = idx;
        }

        // Called by UI to advance to next player action
        public void OnPlayerActionSubmitted()
        {
            var current = GetCurrentPlayer();
            if (current != null)
                actedPlayers.Add(current);

            // Keep pressure up by refilling the board whenever an action kills something
            CombatWaveController.Instance?.TryReplenish();

            // Advance to the next player who hasn't acted this phase
            currentPlayerIndex++;
            while (currentPlayerIndex < players.Count && actedPlayers.Contains(players[currentPlayerIndex]))
                currentPlayerIndex++;

            if (currentPlayerIndex >= players.Count)
            {
                EndPlayerPhase();
            }
        }

        private void EndPlayerPhase()
        {
            currentPlayerIndex = 0;
            currentPhase = BattlePhase.Enemy;

            // Tick player cooldowns
            foreach (var p in players)
                p.TickCooldown();

            // Tick root duration（本阶段施放的定身跳过此次递减，见 EnemyCharacter.SetRooted）
            foreach (var e in enemies)
                e.TickRoot();

            // Execute enemy phase
            RunEnemyPhase();
        }

        private void RunEnemyPhase()
        {
            // Sort enemies by column (leftmost first)
            List<EnemyCharacter> enemyOrder = new List<EnemyCharacter>(enemies);
            enemyOrder.Sort((a, b) => a.columnIndex.CompareTo(b.columnIndex));

            foreach (var enemy in enemyOrder)
            {
                if (BattleEnded) break;
                if (!enemy.IsAlive) continue;

                // 1. 被定身 → 本阶段无法移动（攻击仍按威胁规则结算）
                if (!enemy.isRooted)
                {
                    MoveEnemy(enemy);
                }

                // 2. Attack if in threat zone
                if (enemy.IsInThreatZone)
                {
                    AttackPlayer(enemy);
                }
            }

            EndEnemyPhase();
        }

        private void MoveEnemy(EnemyCharacter enemy)
        {
            int newCol = enemy.columnIndex - enemy.moveSpeed;
            if (newCol < 0) newCol = 0;

            // Check if target cell is occupied in the same row
            var occupant = battleGrid.GetOccupant(newCol, enemy.rowIndex);
            if (occupant != null && occupant != enemy)
            {
                // Stop before collision
                newCol = enemy.columnIndex;
            }

            if (newCol != enemy.columnIndex)
            {
                battleGrid.MoveEnemy(enemy, newCol, enemy.rowIndex);
            }
        }

        private void AttackPlayer(EnemyCharacter enemy)
        {
            PlayerCharacter target = players.Find(p => p.rowIndex == enemy.rowIndex);
            if (target == null) return;

            int damage = enemy.attackPower;
            target.TakeDamage(damage);
            ApplySharedDamage(damage);
            Debug.Log($"Enemy {enemy.name} attacks {target.name} for {damage} damage!");
        }

        public void ApplySharedDamage(int damage)
        {
            sharedCurrentHealth -= damage;
            if (sharedCurrentHealth < 0)
                sharedCurrentHealth = 0;

            if (sharedCurrentHealth <= 0)
            {
                EndBattle(false);
            }
        }

        private void EndEnemyPhase()
        {
            // Remove dead enemies
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (!enemies[i].IsAlive)
                {
                    Destroy(enemies[i].gameObject);
                    RemoveEnemy(enemies[i]);
                }
            }

            // 已失败（血条归零）→ 交由结算面板处理
            if (BattleEnded)
                return;

            // Refill if needed before deciding whether the battle is over
            CombatWaveController.Instance?.TryReplenish();

            // Check win: only when all scheduled waves have spawned and the board is clear
            if (CombatWaveController.Instance != null)
            {
                if (CombatWaveController.Instance.IsFinalWaveCleared())
                {
                    EndBattle(true);
                    return;
                }
            }
            else if (enemies.Count == 0)
            {
                EndBattle(true);
                return;
            }

            // Next round
            roundNumber++;
            currentPhase = BattlePhase.Player;
            currentPlayerIndex = 0;
            actedPlayers.Clear();
        }

        private void EndBattle(bool won)
        {
            if (BattleEnded) return;

            BattleEnded = true;
            BattleWon = won;
            currentPhase = BattlePhase.Resolution;

            Debug.Log(won ? "战斗胜利——在结算面板中选择继续。" : "战斗失败……");
        }

        // 结算面板按钮入口：重新挑战/重新开始
        public void RestartBattle()
        {
            var session = WutaiTactics.Map.GameSession.Instance;
            if (session != null)
                session.ReturningFromBattle = false; // 重试不结算地图进度
            SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        // 结算面板按钮入口：返回地图
        public void ReturnToMap()
        {
            var session = WutaiTactics.Map.GameSession.Instance;
            if (session == null || session.MapData == null) return;

            session.LastBattleWon = BattleWon;
            session.ReturningFromBattle = true;
            SceneManager.LoadScene("MapScene", LoadSceneMode.Single);
        }

        // Public: player normal attack（仅限本行目标）
        public bool PlayerAttack(PlayerCharacter player, EnemyCharacter target)
        {
            if (player == null || target == null || !target.IsAlive) return false;
            if (target.rowIndex != player.rowIndex)
            {
                Debug.Log($"{player.name} 只能攻击本行（第 {player.rowIndex + 1} 行）的敌人");
                return false;
            }

            RegionType targetRegion = battleGrid.GetRegion(target.columnIndex);
            int damage = DamageTable.CalculateDamage(player.attackPower, player.role, targetRegion);

            // 定身：被定身敌人受到双倍矢石伤害
            bool doubled = player.role == CombatRole.Arrow && IsRootedEnemy(target);
            if (doubled) damage *= 2;

            target.TakeDamage(damage);
            Debug.Log($"{player.name} attacks {target.name} for {damage} damage!{(doubled ? "（定身 ×2）" : "")}");

            if (!target.IsAlive)
            {
                Destroy(target.gameObject);
                RemoveEnemy(target);
            }
            return true;
        }

        // Public: player use skill（仅限本行目标）
        public bool PlayerUseSkill(PlayerCharacter player, EnemyCharacter target)
        {
            if (player == null || !player.CanUseSkill) return false;
            if (target == null || !target.IsAlive) return false;
            if (target.rowIndex != player.rowIndex)
            {
                Debug.Log($"{player.name} 的技能只能指定本行（第 {player.rowIndex + 1} 行）的敌人");
                return false;
            }

            player.UseSkill();

            switch (player.skillType)
            {
                case SkillType.Push:
                    SkillPush(target);
                    break;
                case SkillType.Pull:
                    SkillPull(target);
                    break;
                case SkillType.Root:
                    SkillRoot(target);
                    break;
            }
            return true;
        }

        private void SkillPush(EnemyCharacter enemy)
        {
            int newCol = enemy.columnIndex + 1;
            if (newCol >= battleGrid.columns)
                newCol = battleGrid.columns - 1;

            var occupant = battleGrid.GetOccupant(newCol, enemy.rowIndex);
            if (occupant != null && occupant != enemy)
            {
                newCol = enemy.columnIndex; // blocked, can't push
            }

            if (newCol != enemy.columnIndex)
            {
                battleGrid.MoveEnemy(enemy, newCol, enemy.rowIndex);
                Debug.Log($"Pushed {enemy.name} to column {newCol}");
            }
        }

        private void SkillPull(EnemyCharacter enemy)
        {
            int newCol = enemy.columnIndex - 1;
            if (newCol < 0)
                newCol = 0;

            var occupant = battleGrid.GetOccupant(newCol, enemy.rowIndex);
            if (occupant != null && occupant != enemy)
            {
                newCol = enemy.columnIndex; // blocked
            }

            if (newCol != enemy.columnIndex)
            {
                battleGrid.MoveEnemy(enemy, newCol, enemy.rowIndex);
                Debug.Log($"Pulled {enemy.name} to column {newCol}");
            }
        }

        private void SkillRoot(EnemyCharacter enemy)
        {
            enemy.SetRooted(1);
            Debug.Log($"Rooted {enemy.name}");
        }

        public bool IsRootedEnemy(EnemyCharacter enemy)
        {
            return enemy != null && enemy.isRooted;
        }
    }
}
