// GachaSceneController.cs

using System.Collections;
using UnityEngine;

public class GachaSceneController : MonoBehaviour
{
    [SerializeField] private GachaTimelineController timelineController;
    [SerializeField] private GachaResultUI resultUI;
    [SerializeField] private UnityEngine.UI.Button confirmButton;

    void Start()
    {
        //confirmButton.onClick.AddListener(OnConfirm);
        //Camera.main?.gameObject.SetActive(false);
        //StartCoroutine(GachaFlow());
    }

    private void OnEnable()
    {
        GachaContext.OnGachaResult += OnGachaStart;
    }

    private void OnDisable()
    {
        GachaContext.OnGachaResult -= OnGachaStart;
    }

    private void OnGachaStart()
    {
        StartCoroutine(GachaFlow());
    }
    IEnumerator GachaFlow()
    {
        // 1. 공통 연출 끝날 때까지 대기
        yield return StartCoroutine(timelineController.PlayAndWait("common"));

        var results = GachaContext.PendingResults;

        // 2. 전설 있으면 컷씬 재생
        foreach (var item in results)
        {
            if (item.grade == 4)
            {
                var data = GachaRewardDatabase.Instance.Find(item.typeId, item.rewardId);
                if (data is CharacterRewardData charData
                    && !string.IsNullOrEmpty(charData.cutsceneKey))
                {
                    yield return StartCoroutine(
                        timelineController.PlayAndWait(charData.cutsceneKey)
                    );
                }
                break; // 첫 번째 전설만
            }
        }

        // 3. 결과 UI 표시
        resultUI.Show(results);

        // 4. 확인 버튼 활성화
        confirmButton.gameObject.SetActive(true);
    }

    public void OnConfirmClicked()
    {
        GachaSceneLoader.Unload(() =>
        {
            Camera.main?.gameObject.SetActive(true);
            ShopController.Instance.RestoreBanner(GachaContext.CurrentBannerId);
        });
    }
}