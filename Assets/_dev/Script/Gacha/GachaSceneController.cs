using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GachaTimelineController timelineController;
    [SerializeField] private GameObject resultPanelObj;
    [SerializeField] private Button confirmButton;             // 확인 버튼

    void Start()
    {
        confirmButton.onClick.AddListener(OnConfirm);

        // 로비 카메라 끄기
        Camera.main?.gameObject.SetActive(false);

        StartCoroutine(GachaFlow());
    }

    IEnumerator GachaFlow()
    {
        // 1. 공용 연출 재생
        yield return StartCoroutine(timelineController.PlayAndWait("common"));

        // 2. 서버 결과 대기
        yield return StartCoroutine(WaitForResult());

        GachaResult result = GachaContext.PendingResult;

        // 3. 결과에 따라 캐릭터 타임라인 분기
        if (result.IsSpecialCutscene)
        {
            // CharacterId 그대로 key로 사용
            // "char_special_001" → 해당 타임라인 재생
            yield return StartCoroutine(
                timelineController.PlayAndWait(result.CharacterId)
            );
        }

        // 4. 결과 UI
        ShowResult(result);
    }

    IEnumerator WaitForResult()
    {
        float timeout = 10f;
        float elapsed = 0f;
        while (!GachaContext.IsResultReady)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout) yield break;
            yield return null;
        }
    }

    void ShowResult(GachaResult result)
    {
        resultPanelObj.SetActive(true);
    }

    void OnConfirm()
    {
        string panelId = GachaContext.LastShopPanelId;
        GachaSceneLoader.Unload(() =>
        {
            //Camera.main?.gameObject.SetActive(true);
            //ShopManager.Instance.OpenPanel(panelId);
        });
    }
}