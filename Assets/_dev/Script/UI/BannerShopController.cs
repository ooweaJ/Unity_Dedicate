using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BannerShopController : MonoBehaviour
{
    [Header("배너 데이터 목록")]
    [SerializeField] private List<BannerData> bannerList;
    [SerializeField] private string defaultBannerId = "";  // 처음 선택될 배너

    [Header("왼쪽 리스트")]
    [SerializeField] private BannerListItem listItemPrefab;
    [SerializeField] private Transform listContent;   // ScrollView > Content

    [Header("오른쪽 메인 콘텐츠")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI remainTimeText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("전환 설정")]
    [SerializeField] private float fadeDuration = 0.3f;

    private List<BannerListItem> _listItems = new();
    private BannerListItem _selectedItem;
    private BannerData _currentBanner;

    void Start()
    {
        BuildList();

        // 기본 배너 선택
        var defaultBanner = string.IsNullOrEmpty(defaultBannerId)
            ? bannerList[0]
            : bannerList.Find(b => b.bannerId == defaultBannerId) ?? bannerList[0];

        SelectBanner(defaultBanner, animate: false);
    }

    // ── 리스트 생성 ───────────────────────────────────
    void BuildList()
    {
        foreach (Transform child in listContent)
            Destroy(child.gameObject);

        _listItems.Clear();

        foreach (var data in bannerList)
        {
            var item = Instantiate(listItemPrefab, listContent);
            item.Setup(data, OnBannerSelected);
            _listItems.Add(item);
        }
    }

    // ── 버튼 클릭 콜백 ────────────────────────────────
    void OnBannerSelected(BannerData data)
    {
        if (_currentBanner == data) return;  // 같은 배너 클릭 무시
        SelectBanner(data, animate: true);
    }

    // ── 배너 선택 처리 ────────────────────────────────
    void SelectBanner(BannerData data, bool animate)
    {
        _currentBanner = data;

        // 선택 아이템 하이라이트
        foreach (var item in _listItems)
            item.SetSelected(false);

        var selected = _listItems.Find(i => i.GetData() == data);
        if (selected != null)
        {
            selected.SetSelected(true);
            _selectedItem = selected;
        }

        // 콘텐츠 교체
        if (animate)
            StartCoroutine(SwitchContentFade(data));
        else
            ApplyContent(data);
    }

    // ── 페이드 전환 ───────────────────────────────────
    IEnumerator SwitchContentFade(BannerData data)
    {
        // 페이드 아웃
        yield return StartCoroutine(FadeGroup(1f, 0f));

        ApplyContent(data);

        // 페이드 인
        yield return StartCoroutine(FadeGroup(0f, 1f));
    }

    void ApplyContent(BannerData data)
    {
        backgroundImage.sprite = data.backgroundSprite;
        titleText.text = data.bannerTitle;
        remainTimeText.text = data.remainTimeText;
        descriptionText.text = data.descriptionText;

        if (characterImage != null && data.characterSprite != null)
            characterImage.sprite = data.characterSprite;
    }

    IEnumerator FadeGroup(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            SetRightPanelAlpha(alpha);
            yield return null;
        }
        SetRightPanelAlpha(to);
    }

    void SetRightPanelAlpha(float alpha)
    {
        // CanvasGroup이 있으면 사용
        var cg = backgroundImage.transform.parent.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = alpha;
    }

    // ── 상점 열릴 때 패널 복원용 ──────────────────────
    public void OpenToBanner(string bannerId)
    {
        var data = bannerList.Find(b => b.bannerId == bannerId);
        if (data != null) SelectBanner(data, animate: false);
    }
}