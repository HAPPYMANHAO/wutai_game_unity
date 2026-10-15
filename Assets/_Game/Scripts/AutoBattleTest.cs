using UnityEngine;
using System.Collections;
using System.Linq;

namespace WutaiTactics
{
    /// <summary>
    /// 进 Play Mode 自动运行的战斗功能验证（从地图流程进入时自动禁用）：
    /// 回合1 普攻 → 回合2 技能（记录定身目标）→ 校验定身封锁移动/状态存续
    /// → 回合3 矢石攻击被定身目标，校验双倍伤害。
    /// </summary>
    public class AutoBattleTest : MonoBehaviour
    {
        private bool _battleEnded;

        private IEnumerator Start()
        {
            // 只有不处于大地图流程时才自动跑测试
            if (WutaiTactics.Map.GameSession.Instance != null)
            {
                enabled = false;
                yield break;
            }

            yield return new WaitForSeconds(1f);

            var cm = CombatManager.Instance;
            if (cm == null)
            {
                Debug.LogError("[AutoBattleTest] CombatManager not found");
                yield break;
            }

            Debug.Log("--- Auto Battle Test Start ---");
            Debug.Log($"Round {cm.roundNumber}, Phase {cm.currentPhase}, HP {cm.sharedCurrentHealth}/{cm.sharedMaxHealth}");
            LogEnemies(cm);

            // Round 1: 三人普攻本行最近敌人
            Debug.Log("--- Round 1 - Normal Attacks ---");
            for (int i = 0; i < cm.players.Count; i++)
            {
                yield return WaitForTurn(cm);
                if (_battleEnded) yield break;

                var p = cm.GetCurrentPlayer();
                var target = FindNearestInRow(cm, p);

                if (target != null)
                {
                    Debug.Log($"[Auto] {p.role} attacks {target.role} at ({target.columnIndex},{target.rowIndex})");
                    cm.PlayerAttack(p, target);
                }
                else
                {
                    Debug.Log($"[Auto] {p.role} has no target, passes");
                }
                cm.OnPlayerActionSubmitted();
                yield return new WaitForSeconds(0.2f);
            }

            yield return new WaitForSeconds(0.3f);
            Debug.Log($"After Round 1: HP {cm.sharedCurrentHealth}/{cm.sharedMaxHealth}, Enemies alive: {cm.enemies.Count}");
            LogEnemies(cm);

            // Round 2: 三人技能；记录矢石定身目标
            Debug.Log("--- Round 2 - Skills ---");
            EnemyCharacter rootedTarget = null;
            int rootedColBefore = -1;
            int rootedRowBefore = -1;

            for (int i = 0; i < cm.players.Count; i++)
            {
                yield return WaitForTurn(cm);
                if (_battleEnded) yield break;

                var p = cm.GetCurrentPlayer();

                if (!p.CanUseSkill)
                {
                    Debug.Log($"[Auto] {p.role} skill on cooldown, passes");
                    cm.OnPlayerActionSubmitted();
                    continue;
                }

                var target = FindNearestInRow(cm, p);

                if (target != null)
                {
                    Debug.Log($"[Auto] {p.role} uses {p.skillType} on {target.role} at ({target.columnIndex},{target.rowIndex})");
                    bool ok = cm.PlayerUseSkill(p, target);
                    if (ok && p.skillType == SkillType.Root)
                    {
                        rootedTarget = target;
                        rootedColBefore = target.columnIndex;
                        rootedRowBefore = target.rowIndex;
                    }
                }
                else
                {
                    Debug.Log($"[Auto] {p.role} has no target for skill, passes");
                }
                cm.OnPlayerActionSubmitted();
                yield return new WaitForSeconds(0.2f);
            }

            yield return new WaitForSeconds(0.3f);
            Debug.Log($"After Round 2: HP {cm.sharedCurrentHealth}/{cm.sharedMaxHealth}, Enemies alive: {cm.enemies.Count}");
            LogEnemies(cm);

            // Check 1: 定身封锁移动 + 状态延续到下一玩家阶段
            if (rootedTarget != null && rootedTarget.IsAlive)
            {
                bool stayed = rootedTarget.columnIndex == rootedColBefore && rootedTarget.rowIndex == rootedRowBefore;
                Debug.Log($"[Check] ROOT-BLOCK: {(stayed ? "PASS" : "FAIL")} | " +
                          $"pos ({rootedColBefore},{rootedRowBefore}) -> ({rootedTarget.columnIndex},{rootedTarget.rowIndex}) | " +
                          $"isRooted(下一玩家阶段)={rootedTarget.isRooted} (期望 True)");
            }
            else
            {
                Debug.Log("[Check] ROOT-BLOCK: SKIP（无定身目标或目标已死亡）");
            }

            // Round 3: 矢石攻击被定身目标 → 双倍伤害
            Debug.Log("--- Round 3 - Double Damage Check ---");
            if (rootedTarget != null && rootedTarget.IsAlive && cm.currentPhase == BattlePhase.Player)
            {
                var rootedRole = rootedTarget.role;
                int rootedCol = rootedTarget.columnIndex;

                for (int i = 0; i < cm.players.Count; i++)
                {
                    yield return WaitForTurn(cm);
                    if (_battleEnded) yield break;

                    var p = cm.GetCurrentPlayer();

                    if (p.role == CombatRole.Arrow && rootedTarget != null && rootedTarget.IsAlive)
                    {
                        RegionType region = cm.battleGrid.GetRegion(rootedTarget.columnIndex);
                        int before = rootedTarget.currentHealth;
                        bool ok = cm.PlayerAttack(p, rootedTarget);
                        int dealt = ok ? before - rootedTarget.currentHealth : 0;
                        int baseDmg = DamageTable.CalculateDamage(p.attackPower, p.role, region);
                        Debug.Log($"[Check] DOUBLE: 矢石攻击被定身{rootedRole}(col {rootedCol}, 区域 {region}) 造成 {dealt} 伤害，期望 {baseDmg * 2}（未定身应为 {baseDmg}）");
                    }
                    cm.OnPlayerActionSubmitted();
                    yield return new WaitForSeconds(0.2f);
                }
            }
            else
            {
                Debug.Log("[Check] DOUBLE: SKIP");
            }

            yield return new WaitForSeconds(0.3f);
            Debug.Log($"After Round 3: HP {cm.sharedCurrentHealth}/{cm.sharedMaxHealth}, Enemies alive: {cm.enemies.Count}");
            LogEnemies(cm);
            Debug.Log("--- Auto Battle Test End ---");
        }

        /// <summary>等到回到玩家阶段；若战斗已结束（结算）则置 _battleEnded。</summary>
        private IEnumerator WaitForTurn(CombatManager cm)
        {
            yield return new WaitUntil(() => cm.currentPhase == BattlePhase.Player || cm.currentPhase == BattlePhase.Resolution);
            _battleEnded = cm.currentPhase != BattlePhase.Player;
            if (_battleEnded)
                Debug.Log("[Auto] 战斗已结束，提前中止测试");
        }

        private EnemyCharacter FindNearestInRow(CombatManager cm, PlayerCharacter p)
        {
            return cm.enemies
                .Where(e => e != null && e.IsAlive && e.rowIndex == p.rowIndex)
                .OrderBy(e => e.columnIndex)
                .FirstOrDefault();
        }

        private void LogEnemies(CombatManager cm)
        {
            foreach (var e in cm.enemies)
            {
                Debug.Log($"  Enemy {e.role} at ({e.columnIndex},{e.rowIndex}) HP {e.currentHealth}/{e.maxHealth} ThreatZone={e.IsInThreatZone} Rooted={e.isRooted}");
            }
        }
    }
}
