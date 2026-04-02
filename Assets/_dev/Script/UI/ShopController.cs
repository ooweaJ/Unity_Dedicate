using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class ShopController : MonoBehaviour
{
    static public ShopController Instance;
    [Header("배너 데이터")]
    [SerializeField] private List<BannerData> bannerDataList;

    [Header("리스트 영역")]
    [SerializeField] private BannerListItem listItemPrefab;
    [SerializeField] private Transform listContent;

    [Header("메인 콘텐츠")]
    [SerializeField] private Image illustrationImage;
    [SerializeField] private TextMeshProUGUI bannerTitleText;
    [SerializeField] private TextMeshProUGUI remainTimeText;

    [Header("네임카드")]
    [SerializeField] private CharacterNamePlate namePlate;

    [SerializeField] private ShopUI shopUI;
    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private Camera lobbyCamera;

    // bannerId → BannerListItem 맵
    private Dictionary<int, BannerListItem> _itemMap = new();
    private int _currentBannerId = -1;

    void Awake()
    {
        BuildList();

        if (bannerDataList.Count > 0)
            SelectBanner(bannerDataList[0]);
    }

    private void OnEnable()
    {
        if (shopUI != null)
        {
            shopUI.OnClikedGachaButton += OnClickGacha;
        }
    }
  async void OnClickGacha(int amount)
    {
        GachaContext.CurrentBannerId = _currentBannerId;
        GachaContext.PullAmount = amount;
        GachaContext.Clear();

        if (lobbyCamera != null)
            lobbyCamera.gameObject.SetActive(false);

        shopUI.Close();
        lobbyUI.Close();

        // 씬 먼저 활성화 (연출 바로 시작)
        GachaSceneLoader.Activate();

        // 서버 요청 (연출 도중 응답 옴)
        string json = await BackendManager.GachaDraw(
            userId: PlayerDataManager.Instance.GetUserId(),
            bannerId: _currentBannerId,
            amount: amount
        );

        var response = JsonUtility.FromJson<GachaPullResponse>(json);
        if (response.success)
        {
            GachaContext.PendingResults = response.results;
            GachaContext.IsResultReady = true;
            GachaContext.OnGachaResult.Invoke();
        }
    }
    void BuildList()
    {
        foreach (Transform child in listContent)
            Destroy(child.gameObject);

        _itemMap.Clear();

        foreach (var data in bannerDataList)
        {
            var item = Instantiate(listItemPrefab, listContent);
            item.Setup(data, SelectBanner);
            _itemMap[data.bannerId] = item;   // ID로 등록
        }
    }

    void SelectBanner(BannerData data)
    {
        if (_currentBannerId == data.bannerId) return;
        _currentBannerId = data.bannerId;

        // 전체 오버레이 켜고
        foreach (var item in _itemMap.Values)
            item.SetSelected(false);

        // 선택된 것만 끄기
        if (_itemMap.TryGetValue(data.bannerId, out var selected))
            selected.SetSelected(true);

        // 콘텐츠 갱신
        illustrationImage.sprite = data.illustrationSprite;
        bannerTitleText.text = data.bannerTitle;
        remainTimeText.text = data.remainTime;
        namePlate.Setup(data);

        //GachaContext.LastShopPanelId = data.bannerId.ToString();
    }

    // 가챠 씬 복귀 시 호출
    public void RestoreBanner(int bannerId)
    {
        var data = bannerDataList.Find(b => b.bannerId == bannerId);
        if (data != null) SelectBanner(data);
    }
}