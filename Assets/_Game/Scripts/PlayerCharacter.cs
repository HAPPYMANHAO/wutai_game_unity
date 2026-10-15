using UnityEngine;

namespace WutaiTactics
{
    public class PlayerCharacter : CombatActor
    {
        [Header("Skill")]
        public SkillType skillType;
        public int skillCooldownMax = 3;
        public int currentSkillCooldown = 0;

        [Header("Position")]
        public int rowIndex; // 0,1,2

        public bool CanUseSkill => currentSkillCooldown <= 0;

        public void UseSkill()
        {
            if (CanUseSkill)
            {
                currentSkillCooldown = skillCooldownMax;
            }
        }

        public void TickCooldown()
        {
            if (currentSkillCooldown > 0)
                currentSkillCooldown--;
        }
    }
}
