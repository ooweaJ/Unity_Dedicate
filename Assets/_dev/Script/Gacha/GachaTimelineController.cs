using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class GachaTimelineController : MonoBehaviour
{
    [System.Serializable]
    public class TimelineEntry
    {
        public string key;       // "common", "char_001", "char_002"
        public PlayableDirector director;
    }

    [SerializeField] private List<TimelineEntry> timelines;

    // key로 타임라인 찾기
    private PlayableDirector GetDirector(string key)
    {
        return timelines.Find(t => t.key == key)?.director;
    }

    // ── 타임라인 재생 후 끝날 때까지 대기 ─────────────
    public IEnumerator PlayAndWait(string key)
    {
        var director = GetDirector(key);

        if (director == null)
        {
            Debug.LogWarning($"[Timeline] 키 없음: {key}");
            yield break;
        }

        director.gameObject.SetActive(true);
        director.time = 0;
        director.Play();

        // 재생 완료까지 대기
        yield return new WaitUntil(() =>
            director.state != PlayState.Playing);

        director.gameObject.SetActive(false);
        Debug.Log($"[Timeline] 완료: {key}");
    }
}