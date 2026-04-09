using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform comsumcontent;
    [SerializeField] private Transform Charactercontent;
    [SerializeField] private Transform CharacterSharedcontent;
    [SerializeField] private Transform Equipcontent;
    [SerializeField] private CharacterSlot CharacterslotPrefab;
    // 소비 슬롯?, 장비 슬롯? , 초월 재료 슬롯? 다만들어야하나? 하나로 관리할 수 있나?


    public void Init(IEnumerable<PlayerCharacterData> playerCharacterDatas, IEnumerable<PlayerItemData> playerItemDatas)
    {
        foreach (var pc in playerCharacterDatas)
        {
        }
    }
    //public void RefreshItems(IEnumerable<UserItemData> playerItems)
    //{
    //    // 1. 기존 슬롯 싹 비우기
    //    foreach (Transform child in content)
    //        Destroy(child.gameObject);

    //    // 2. 아이템 데이터 순회하며 슬롯 생성
    //    foreach (var pi in playerItems)
    //    {

    //    }
    //}

    //public void Refresh(IEnumerable<PlayerDatas> playerCharacterDatas)
    //{
    //    foreach (Transform child in content)
    //        Destroy(child.gameObject);

    //    foreach (var pc in playerCharacterDatas)
    //    {
    //        var baseData = characterDatabase.GetById(pc.characterId);
    //        var slot = Instantiate(slotPrefab, content);
    //        slot.Set(pc, baseData);
    //    }
    //}

    //public void Open()
    //{
    //    panel.SetActive(true);
    //}

    //public void Close()
    //{
    //    panel.SetActive(false);
    //}
}
