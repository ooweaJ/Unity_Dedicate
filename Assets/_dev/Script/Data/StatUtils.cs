using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 모든 캐릭터 스탯 계산의 "유일한 기준"이 되는 유틸리티
/// 로비 UI, 배틀 서버 모두 여기서 계산합니다.
/// </summary>
public static class StatUtils
{
    public struct CalculatedStats
    {
        public float atk;
        public float def;
        public float hp;
    }

    public static CalculatedStats Calculate(CharacterRawData baseData, int level, IEnumerable<int> equippedItemIds)
    {
        if (baseData == null) return default;

        // 1. 레벨업 보너스 계산 (공식 통일: 1레벨은 보너스 0)
        float levelBonusAtk = baseData.baseAtk * (level - 1) * 0.1f;
        float levelBonusDef = baseData.baseDef * (level - 1) * 0.05f;
        float levelBonusHp  = baseData.baseHp  * (level - 1) * 0.1f;

        // 2. 장비 보너스 합산
        float itemBonusAtk = 0;
        float itemBonusDef = 0;
        float itemBonusHp  = 0;

        if (equippedItemIds != null)
        {
            foreach (int itemId in equippedItemIds)
            {
                var item = GameDataManager.Instance.GetItem(itemId);
                if (item != null)
                {
                    itemBonusAtk += item.atkBonus;
                    itemBonusDef += item.defBonus;
                    // itemBonusHp += item.hpBonus; // 추후 확장 대비
                }
            }
        }

        return new CalculatedStats
        {
            atk = baseData.baseAtk + levelBonusAtk + itemBonusAtk,
            def = baseData.baseDef + levelBonusDef + itemBonusDef,
            hp  = baseData.baseHp  + levelBonusHp  + itemBonusHp
        };
    }
}
