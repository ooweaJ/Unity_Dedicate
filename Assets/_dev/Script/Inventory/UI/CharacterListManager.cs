using System.Collections.Generic;
using UnityEngine;

public class CharacterListManager : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform contentParent;

    private List<CharacterSlot> _slots = new();
    private System.Action<int> _onCharacterSelected; // 외부(UIManager)로 보낼 최종 신호

    // 생성할 때 '어떤 캐릭터가 처음 선택되어 있을지'도 받으면 센스 만점!
    public void Init(List<CharacterUIModel> models, int initialId, System.Action<int> onSelect)
    {
        _onCharacterSelected = onSelect;

        // 1. 기존 슬롯 청소
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        _slots.Clear();

        // 2. 슬롯 생성 및 "부모 바인딩"
        foreach (var m in models)
        {
            var go = Instantiate(slotPrefab, contentParent);
            var slot = go.GetComponent<CharacterSlot>();

            // 여기서 슬롯에게 "클릭되면 나(Manager)한테 먼저 말해!"라고 로직을 넘깁니다.
            slot.Setup(m, (clickedId) => {
                OnSlotClickedInternal(clickedId);
            });

            _slots.Add(slot);
        }

        // 3. 시작하자마자 첫 번째 놈 테두리 켜주기
        RefreshSelection(initialId);
    }

    // 슬롯이 클릭되었을 때 Manager가 수행할 내부 로직
    private void OnSlotClickedInternal(int id)
    {
        // 1. 시각적 리프레시 (나만 노란색, 나머지는 흰색)
        RefreshSelection(id);

        // 2. 부모(InventoryUIManager)에게 "진짜로 캐릭터 바뀌었음!"이라고 최종 보고
        _onCharacterSelected?.Invoke(id);
    }

    public void RefreshSelection(int selectedId)
    {
        // 전체 리스트를 돌면서 ID가 맞는 놈만 선택 상태로 만듭니다.
        foreach (var s in _slots)
        {
            s.SetSelect(s.Id == selectedId);
        }
    }
}