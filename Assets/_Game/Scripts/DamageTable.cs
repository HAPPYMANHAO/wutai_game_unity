using UnityEngine;

namespace WutaiTactics
{
    public static class DamageTable
    {
        // Rows: attacker role, Columns: target region
        // 0=Melee, 1=Polearm, 2=Arrow
        private static readonly float[,] Multipliers = new float[,]
        {
            { 1.0f, 0.5f, 0.2f }, // Melee attacking Melee/Polearm/Arrow
            { 0.5f, 1.0f, 0.5f }, // Polearm attacking Melee/Polearm/Arrow
            { 0.2f, 0.5f, 1.0f }  // Arrow attacking Melee/Polearm/Arrow
        };

        public static float GetMultiplier(CombatRole attacker, RegionType targetRegion)
        {
            return Multipliers[(int)attacker, (int)targetRegion];
        }

        public static int CalculateDamage(int baseAttack, CombatRole attacker, RegionType targetRegion)
        {
            float multiplier = GetMultiplier(attacker, targetRegion);
            return Mathf.RoundToInt(baseAttack * multiplier);
        }
    }
}
