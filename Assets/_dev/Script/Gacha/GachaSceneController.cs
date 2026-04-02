// GachaSceneController.cs

using System.Collections;
using UnityEngine;

public class GachaSceneController : MonoBehaviour
{
    [SerializeField] private GachaTimelineController timelineController;
    //[SerializeField] private GachaResultUI resultUI;
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
        // 1. 공통 연출 (서버 응답 기다리는 동안 재생)
        timelineController.PlayAndWait("common");
    }

    IEnumerator GachaFlow()
    {
        // 2. 결과 대기 (연출 끝났는데 아직 안 왔으면 대기)
        float timeout = 10f, elapsed = 0f;
        while (!GachaContext.IsResultReady)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout) yield break;
            yield return null;
        }

        var results = GachaContext.PendingResults;

        // 3. 전설 등급 있으면 컷씬 재생
        foreach (var item in results)
        {
            if (item.IsLegendary)
            {
                var data = GachaRewardDatabase.Instance.Find(item.typeId, item.rewardId);
                if (data is CharacterRewardData charData && !string.IsNullOrEmpty(charData.cutsceneKey))
                    yield return StartCoroutine(timelineController.PlayAndWait(charData.cutsceneKey));
                break; // 첫 번째 전설만 컷씬
            }
        }

        // 4. 결과 UI 표시
        //resultUI.Show(results);
    }

    void OnConfirm()
    {
        GachaSceneLoader.Unload(() =>
        {
            Camera.main?.gameObject.SetActive(true);
            ShopController.Instance.RestoreBanner(GachaContext.CurrentBannerId);
        });
    }
}