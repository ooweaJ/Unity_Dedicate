using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct TabPanelMapping
{
    public int tabId;
    public List<GameObject> panelsToShow; // 켜질 것만 관리
}

public class InventoryUIManager : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [Header("Sub Managers")]
    [SerializeField] private CharacterListManager listManager;   // 오른쪽 리스트 관리
    [SerializeField] private CharacterModelManager modelManager; // 중앙 3D 모델 관리
    [SerializeField] private CharacterInfoPanel infoPanel;       // 왼쪽 스탯/상세정보 관리
    [SerializeField] private SidebarManager sidebarManager;     // 왼쪽 탭 관리

    [Header("Tab Panels")]
    [SerializeField] private List<TabPanelMapping> panelMappings;

    private List<CharacterUIModel> _characterUIModels = new();
    private Dictionary<int, TabPanelMapping> _panelDict = new();

    private void OnEnable()
    {
        foreach (var mapping in panelMappings)
            _panelDict[mapping.tabId] = mapping;

        sidebarManager.OnTabChanged += SwitchSubPanel;
        sidebarManager.Init();
    }

    private void OnDisable()
    {
        sidebarManager.OnTabChanged -= SwitchSubPanel;
    }

    public void Open() { panel.SetActive(true); }
    public void Close() { panel.SetActive(false); modelManager.ClearAllModels(); }
    public void InitInventory(IEnumerable<PlayerCharacterData> charDatas, IEnumerable<PlayerItemData> itemDatas)
    {
        // 1. 데이터를 UI용 모델로 가공 (서버 데이터 + SO 정적 데이터)
        _characterUIModels = charDatas.Select(s =>
            new CharacterUIModel(s, GameDataManager.Instance.GetCharacter(s.characterId))
        ).ToList();

        // 2. 리스트 매니저에게 슬롯 생성을 시킴 (클릭 콜백 전달)
        listManager.Init(_characterUIModels, 1 , OnSelectCharacter);

        // 3. 초기 선택 처리 (첫 번째 캐릭터가 있다면 자동 선택)
        if (_characterUIModels.Count > 0)
        {
            OnSelectCharacter(_characterUIModels[0].StaticData.id);
        }
    }

    private void SwitchSubPanel(int tabId)
    {
        // 전체 패널 다 끄고
        foreach (var mapping in _panelDict.Values)
            mapping.panelsToShow.ForEach(p => p.SetActive(false));

        // 해당 탭 것만 켜기
        if (_panelDict.TryGetValue(tabId, out var target))
            target.panelsToShow.ForEach(p => p.SetActive(true));
    }

    /// 캐릭터가 선택되었을 때(슬롯 클릭 등) 모든 UI를 갱신하는 함수
    private void OnSelectCharacter(int id)
    {
        var selectedModel = _characterUIModels.FirstOrDefault(m => m.StaticData.id == id);
        if (selectedModel == null) return;

        // 중앙 모델 교체 지시
        modelManager.ShowModel(selectedModel);

        // 정보 패널(스탯 등) 갱신 지시
        infoPanel.SetData(selectedModel);

        // 리스트 매니저에게 '선택 테두리' 갱신 지시
        listManager.RefreshSelection(id);
    }
}