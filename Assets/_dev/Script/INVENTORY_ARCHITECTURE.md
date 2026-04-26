# 인벤토리 UI 아키텍처

## 클래스 역할 요약

| 클래스 | 역할 |
|--------|------|
| `InventoryController` | **브릿지**: 데이터(PlayerInventory) ↔ UI(InventoryUIManager) 연결. 이벤트 구독/해제 담당 |
| `InventoryUIManager` | **UI 총괄**: 하위 매니저들을 조율, 데이터 캐싱, 탭/슬롯 이벤트를 외부 이벤트로 변환 |
| `PlayerInventory` | **데이터 모델**: 캐릭터·아이템·장착 정보 보관. 변경 시 `OnChanged` 이벤트 발행 |
| `PlayerDataManager` | **싱글톤**: `PlayerInventory`를 포함한 `PlayerData` 소유자 |
| `GameDataManager` | **싱글톤**: SO 테이블(`ItemTableSO`, `CharacterTableSO`) → O(1) 조회 |

---

## 인벤토리 열기 (OpenInventory)

```
LobbyUI.OnInventoryButtonClicked
  └─ InventoryController.OpenInventory()
       ├─ InventoryUIManager.Open()              → panel.SetActive(true)
       └─ InventoryController.RefreshUI()
            └─ InventoryUIManager.InitInventory(characters, items)
                 ├─ [캐시] _cachedCharacterDatas  ← PlayerInventory.GetAllCharacters()
                 ├─ [캐시] _cachedItemDatas       ← PlayerInventory.GetAllItems()
                 ├─ [빌드] _characterUIModels     ← CharacterUIModel(PlayerCharacterData, CharacterRawData)
                 ├─ CharacterListManager.Init()   → 캐릭터 리스트 슬롯 생성
                 └─ OnSelectCharacter(첫 번째 or 이전 선택 캐릭터 id)
                      ├─ modelManager.ShowModel()        → 3D 모델 표시
                      ├─ infoPanel.SetData(model)        → 레벨/HP/ATK/DEF 표시
                      ├─ listManager.RefreshSelection()  → 선택 하이라이트
                      ├─ RefreshEquipmentSlots()         → 장착 슬롯 표시
                      └─ RefreshActiveItemUI()           → 아이템 그리드 갱신
```

---

## 탭 전환 (SwitchSubPanel)

```
TabButton 클릭
  └─ SidebarManager.HandleTabClicked()
       └─ SidebarManager.OnTabChanged(tabId)
            └─ InventoryUIManager.SwitchSubPanel(tabId)
                 ├─ 모든 패널 SetActive(false)
                 ├─ 해당 tabId 패널 SetActive(true)
                 ├─ itemInfoPanel.Hide()
                 └─ actionPopup.Hide()

※ 탭 전환 후 아이템 그리드는 InventoryUI.OnEnable()에서 자동 Refresh() 호출됨
```

---

## 장비 장착 (드래그 & 드롭)

```
InventorySlot.OnBeginDrag()
  └─ [장비 타입만] OnDragBegin(itemId) 이벤트
       └─ InventoryUIManager.HandleItemDragBegin(itemId)
            └─ DragController.BeginDrag(ItemRawData)
                 └─ dragIcon 활성화 + raycastTarget = false

InventorySlot.OnDrag()
  └─ DragController.OnDrag(screenPos)  → dragIcon 위치 이동

EquipmentSlot.OnDrop()
  ├─ DragController.DraggingData 검증 (타입, slotType 일치 확인)
  ├─ OnItemDropped(itemId, slotType) 이벤트
  │    └─ InventoryUIManager.HandleEquip(itemId, slotType)
  │         └─ OnEquipItem(charId, itemId, slotType) 이벤트
  │              └─ InventoryController.HandleEquipItem(charId, itemId, slotType)
  │                   └─ PlayerInventory.EquipItem(charId, itemId, slot)
  │                        ├─ 기존 장착 아이템 → ReturnItemToInventory() (교체 시)
  │                        ├─ equippedItems[slot] = itemId  [데이터 저장]
  │                        ├─ RemoveItemFromInventory(itemId)  [그리드에서 제거]
  │                        └─ OnChanged 이벤트 발행
  │                             └─ InventoryController.HandleRefresh()
  │                                  └─ RefreshUI() → InitInventory() → OnSelectCharacter()
  │                                       ├─ RefreshEquipmentSlots()  → 슬롯에 아이템 표시
  │                                       ├─ infoPanel.SetData()      → ATK/DEF 수치 갱신
  │                                       └─ RefreshActiveItemUI()    → 그리드 재생성
  │
  └─ DragController.EndDrag()  → dragIcon 비활성화

InventorySlot.OnEndDrag()
  └─ DragController.EndDrag()  (안전 중복 호출, 무해)
```

---

## 장비 해제 (클릭)

```
EquipmentSlot.OnPointerClick()
  └─ [비어있지 않을 때] OnItemClicked(itemId) 이벤트
       └─ InventoryUIManager.HandleUnequip(itemId)
            ├─ _equipmentSlots에서 해당 itemId 슬롯 검색 → slotType 파악
            └─ OnUnequipItem(charId, slotType) 이벤트
                 └─ InventoryController.HandleUnequipItem(charId, slotType)
                      └─ PlayerInventory.UnequipItem(charId, slot)
                           ├─ equippedItems.Remove(slot)
                           ├─ ReturnItemToInventory(itemId)  [그리드로 복귀]
                           └─ OnChanged → (장착과 동일한 RefreshUI 흐름)
```

---

## 소비아이템 클릭 (팝업)

```
InventorySlot.OnPointerClick()
  └─ [장비 타입 제외] OnClicked(itemId, pos) 이벤트
       └─ InventoryUIManager.HandleItemClicked(itemId, pos)
            ├─ GameDataManager.GetItem(itemId) → ItemRawData 조회
            └─ [Equipment 아닌 타입] ItemActionPopup.Show(data, pos, callback)
                 └─ data.actions 리스트 기반으로 버튼 동적 생성
                      └─ 버튼 클릭 시 → HandleAction(itemId, actionType)
                           ├─ Use          → OnUseItem(charId, itemId) → InventoryController.HandleUseItem()

                           ├─ Discard      → OnDiscardItem(itemId) → InventoryController.HandleDiscardItem()
                           └─ Transcendence→ OnOpenTranscendence(itemId) → InventoryController.HandleOpenTranscendence()
```

---

## 데이터 실시간 갱신 흐름 (OnChanged)

```
PlayerInventory.OnChanged 발행
  └─ InventoryController.HandleRefresh()
       └─ InventoryUIManager.InitInventory(GetAllCharacters(), GetAllItems())
            (인벤토리 열려있으면 UI 즉시 갱신)
```

---

## 캐시 위치 정리

| 데이터 | 보관 위치 | 갱신 시점 |
|--------|-----------|-----------|
| 캐릭터 런타임 데이터 (레벨/경험치/장착) | `PlayerInventory.characters` (Dictionary) | `ApplyCharacters()` 또는 `EquipItem()` 호출 시 |
| 아이템 런타임 데이터 (보유량) | `PlayerInventory.items` (Dictionary) | `ApplyItems()` 또는 `EquipItem()` 호출 시 |
| UI용 캐릭터 리스트 | `InventoryUIManager._cachedCharacterDatas` | `InitInventory()` 호출마다 |
| UI용 아이템 리스트 | `InventoryUIManager._cachedItemDatas` | `InitInventory()` 호출마다 |
| UI 모델 래퍼 | `InventoryUIManager._characterUIModels` | `InitInventory()` 호출마다 |
| 선택된 캐릭터 ID | `InventoryUIManager._selectedCharacterId` | `OnSelectCharacter()` 호출 시 (새로고침 시 유지됨) |
| 아이템 정적 데이터 | `GameDataManager` → `ItemTableSO` (Dict) | 앱 시작 시 1회 (SO 역직렬화) |
| 캐릭터 정적 데이터 | `GameDataManager` → `CharacterTableSO` (Dict) | 앱 시작 시 1회 (SO 역직렬화) |

---

## 스탯 계산 (CharacterUIModel)

```csharp
Atk = CharacterRawData.baseAtk + Σ(equippedItems의 각 ItemRawData.atkBonus)
Def = CharacterRawData.baseDef + Σ(equippedItems의 각 ItemRawData.defBonus)
```

> `CharacterInfoPanel.SetData(model)` 호출 시 매번 계산됨 (캐싱 없음)

---

## 버그 체크리스트

- [ ] `ItemTableSO` Inspector에서 장비 아이템의 `atkBonus` / `defBonus` 값이 0보다 큰지 확인
- [ ] `DragController` GameObject가 씬에 존재하는지, `dragIcon` 필드가 연결되어 있는지 확인
- [ ] `EquipmentSlot`의 `acceptedSlotType`이 아이템의 `slotType`과 일치하는지 확인
- [ ] `InventoryController`가 비활성화 상태로 시작하지 않는지 확인 (OnEnable이 PlayerDataManager 초기화 이후 실행되어야 함)
- [ ] Console에 `장착 아이템 ID X가 ItemTableSO에 없습니다` 경고가 찍히는지 확인
