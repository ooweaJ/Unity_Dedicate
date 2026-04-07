using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private CharacterSlot slotPrefab;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private GameObject panel;

    public void Init(IEnumerable<PlayerCharacterData> playerCharacterDatas)
    {
        foreach (var pc in playerCharacterDatas)
        {
            CharacterData baseData =
                characterDatabase.GetById(pc.characterId);

            CharacterSlot slot = Instantiate(slotPrefab, content);
            slot.GetComponent<CharacterSlot>().Set(pc, baseData);
        }
    }
    public void RefreshItems(IEnumerable<UserItemData> playerItems)
    {
        // 1. 기존 슬롯 싹 비우기
        foreach (Transform child in content)
            Destroy(child.gameObject);

        // 2. 아이템 데이터 순회하며 슬롯 생성
        foreach (var pi in playerItems)
        {
            var baseData = itemDatabase.GetById(pi.itemId);
            if (baseData == null) continue;

            CharacterSlot slot = Instantiate(slotPrefab, content);

            // CharacterSlot의 Set 함수를 호출 (아이템 아이콘과 수량 표시)
            // 여기서 pi.amount는 서버에서 준 아이템 개수
            slot.SetItem(baseData.icon, pi.amount);
        }
    }

    public void Refresh(IEnumerable<PlayerCharacterData> playerCharacterDatas)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var pc in playerCharacterDatas)
        {
            var baseData = characterDatabase.GetById(pc.characterId);
            var slot = Instantiate(slotPrefab, content);
            slot.Set(pc, baseData);
        }
    }

    public void Open()
    {
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }
}
