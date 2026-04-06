// GachaSceneController.cs

using System.Collections;
using UnityEngine;

public class GachaSceneController : MonoBehaviour
{
    [SerializeField] private GachaTimelineController timelineController;
    [SerializeField] private GachaResultUI resultUI;

    void Start()
    {
    }

    private void OnEnable()
    {
        GachaContext.OnGachaResult += OnGachaStart;
        resultUI.ConfirmClicked += OnConfirmClicked;
    }

    private void OnDisable()
    {
        GachaContext.OnGachaResult -= OnGachaStart;
        resultUI.ConfirmClicked -= OnConfirmClicked;
    }

    private void OnGachaStart()
    {
        StartCoroutine(GachaFlow());
    }
    IEnumerator GachaFlow()
    {
        // 미리 그리기
        resultUI.Setup();

        // 공통 연출 끝날 때까지 대기
        yield return StartCoroutine(timelineController.PlayAndWait("common"));

        var results = GachaContext.PendingResults;

        // 전설 있으면 컷씬 재생
        foreach (var item in results)
        {
            if (item.grade == 4)
            {
                var data = GachaRewardDatabase.Instance.Find(item.typeId, item.rewardId);
                if (data.playcutscene
                    && !string.IsNullOrEmpty(data.cutsceneKey))
                {
                    yield return StartCoroutine(
                        timelineController.PlayAndWait(data.cutsceneKey)
                    );
                }
                break; // 첫 번째 전설만
            }
        }

        // 결과 UI 표시
        resultUI.open();
    }

    public void OnConfirmClicked()
    {
        resultUI.close();
        GachaSceneLoader.Unload(() =>
        {
            ShopController.Instance.ReturnToShop();
        });
    }
}