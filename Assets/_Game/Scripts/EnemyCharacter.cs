using UnityEngine;

namespace WutaiTactics
{
    public class EnemyCharacter : CombatActor
    {
        [Header("Enemy")]
        public int moveSpeed = 1; // cells per turn
        public bool isRooted = false;
        public int rootTurnsRemaining = 0;

        [Header("Grid Position")]
        public int rowIndex; // 0,1,2
        public int columnIndex; // 0..5

        protected override void Awake()
        {
            base.Awake();
            EnsureCollider();
        }

        private void EnsureCollider()
        {
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.9f, 0.9f);
            }
        }

        public bool IsInThreatZone
        {
            get
            {
                RegionType currentRegion = BattleGrid.Instance.GetRegion(columnIndex);
                RegionType threatRegion = RoleToRegion(role);
                return currentRegion == threatRegion;
            }
        }

        public static RegionType RoleToRegion(CombatRole role)
        {
            return (RegionType)(int)role;
        }

        /// <summary>
        /// 定身 turns 个敌方阶段（Lv1 = 1）。
        /// 计数器在玩家阶段结束时递减（CombatManager.EndPlayerPhase），
        /// 因此 +1 保证本阶段施放的定身存活过本次递减，封锁紧接着的敌方阶段移动；
        /// 定身状态持续到下一次玩家阶段结束，期间被定身敌人受双倍矢石伤害。
        /// </summary>
        public void SetRooted(int turns)
        {
            isRooted = true;
            rootTurnsRemaining = turns + 1;
        }

        public void TickRoot()
        {
            if (rootTurnsRemaining > 0)
            {
                rootTurnsRemaining--;
                if (rootTurnsRemaining <= 0)
                    isRooted = false;
            }
        }

        public void SetGridPosition(int col, int row)
        {
            columnIndex = col;
            rowIndex = row;
        }
    }
}
