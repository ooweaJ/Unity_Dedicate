using System.Collections.Generic;

/// <summary>
/// 모든 캐릭터 스탯 계산의 "유일한 기준"
/// 로비 UI, 배틀 서버 모두 여기서 계산합니다.
/// </summary>
public static class StatUtils
{
    // 장비 강화 1단계당 기본 보너스의 10% 추가
    private const float ENHANCE_BONUS_PER_LEVEL = 0.1f;

    public struct CalculatedStats
    {
        public float atk;
        public float def;
        public float hp;
    }

    public static CalculatedStats Calculate(
        CharacterRawData baseData,
        int level,
        IEnumerable<PlayerEquipmentData> equippedItems)
    {
        if (baseData == null) return default;

        // 1. 레벨 보너스 (1레벨은 보너스 0)
        float levelBonusAtk = baseData.baseAtk * (level - 1) * baseData.atkUpgradeBonus;
        float levelBonusDef = baseData.baseDef * (level - 1) * baseData.defUpgradeBonus;
        float levelBonusHp  = baseData.baseHp  * (level - 1) * baseData.hpUpgradeBonus;

        // 2. 장비 보너스 (기본값 + 강화 배율)
        float itemBonusAtk = 0;
        float itemBonusDef = 0;

        if (equippedItems != null)
        {
            foreach (var equip in equippedItems)
            {
                var item = GameDataManager.Instance.GetItem(equip.itemId);
                if (item == null) continue;

                float enhanceMultiplier = 1f + equip.enhance * ENHANCE_BONUS_PER_LEVEL;
                itemBonusAtk += item.atkBonus * enhanceMultiplier;
                itemBonusDef += item.defBonus * enhanceMultiplier;
            }
        }

        return new CalculatedStats
        {
            atk = baseData.baseAtk + levelBonusAtk + itemBonusAtk,
            def = baseData.baseDef + levelBonusDef + itemBonusDef,
            hp  = baseData.baseHp  + levelBonusHp
        };
    }
}
