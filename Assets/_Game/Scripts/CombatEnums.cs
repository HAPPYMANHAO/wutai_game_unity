namespace WutaiTactics
{
    public enum CombatRole
    {
        Melee,    // 近战
        Polearm,  // 长柄
        Arrow     // 矢石
    }

    public enum RegionType
    {
        Melee,    // 近战区
        Polearm,  // 长柄区
        Arrow     // 矢石区
    }

    public enum SkillType
    {
        Push,     // 击退
        Pull,     // 拉近
        Root      // 定身
    }

    public enum BattlePhase
    {
        Player,
        Enemy,
        Resolution
    }

    public enum ActionType
    {
        None,
        NormalAttack,
        UseSkill
    }
}
