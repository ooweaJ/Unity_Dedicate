using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryUI inventoryUI;

    private PlayerInventory _inventory;

    void Start()
    {
        // 씬 시작 시점에 안전하게 초기화
        Init();
    }

    private void OnDestroy()
    {
        // 이벤트 해제는 메모리 누수 방지를 위해 필수!
        if (_inventory != null)
            _inventory.OnChanged -= HandleRefresh;
    }

    private void Init()
    {
        // 1. 싱글톤 인스턴스 체크
        if (PlayerDataManager.Instance == null) return;

        // 2. 리팩토링된 이름인 GetInventory()로 가져오기
        _inventory = PlayerDataManager.Instance.GetInventory();

        if (_inventory != null)
        {
            // 3. 이벤트 구독
            _inventory.OnChanged -= HandleRefresh; // 중복 구독 방지
            _inventory.OnChanged += HandleRefresh;

            // 4. 초기 UI 세팅 (캐릭터 목록)
            var data = _inventory.GetAllCharacters();
            inventoryUI.Init(data);
        }
    }

    private void HandleRefresh()
    {
        if (_inventory == null) return;

        // 데이터가 변경되었을 때 UI 리프레시
        var data = _inventory.GetAllCharacters();
        inventoryUI.Refresh(data);
    }

    // 💡 팁: 아이템 UI도 있다면 이런 식으로 확장 가능합니다.
    public void ShowItems()
    {
        var itemData = _inventory.GetAllItems();
        // inventoryUI.ShowItems(itemData); 
    }
}