using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GachaSceneLoader : MonoBehaviour
{
    private static GachaSceneLoader _instance;
    private const string GACHA_SCENE = "GachaScene";

    private AsyncOperation _preloadOp = null;   // 미리 로드해둔 작업
    private bool _isPreloaded = false;

    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 로비 진입하자마자 백그라운드에서 로드 시작
        StartCoroutine(PreloadRoutine());
    }

    // ── 백그라운드 Preload (활성화 X) ─────────────────
    IEnumerator PreloadRoutine()
    {
        _preloadOp = SceneManager.LoadSceneAsync(GACHA_SCENE, LoadSceneMode.Additive);

        // 핵심: false 로 설정하면 로드는 하되 활성화 안 함
        _preloadOp.allowSceneActivation = false;

        // progress 가 0.9f 에 도달하면 로드 완료 (유니티 특성)
        // allowSceneActivation = false 일 때 0.9에서 멈춤
        while (_preloadOp.progress < 0.9f)
        {
            yield return null;
        }

        _isPreloaded = true;
        Debug.Log("[GachaLoader] Preload 완료 - 대기 중");
    }

    // ── 버튼 클릭 시 즉시 활성화 ─────────────────────
    public static void Activate()
    {
        if (_instance._isPreloaded)
        {
            // 이미 로드됨 → 즉시 활성화
            _instance._preloadOp.allowSceneActivation = true;
            _instance.StartCoroutine(_instance.OnActivateComplete());
        }
        else
        {
            // 아직 로드 중 → 로드 끝나자마자 활성화
            Debug.Log("[GachaLoader] 아직 로드 중, 완료되면 바로 활성화");
            _instance.StartCoroutine(_instance.WaitAndActivate());
        }
    }

    IEnumerator WaitAndActivate()
    {
        // 로드 완료까지 대기
        yield return new WaitUntil(() => _instance._isPreloaded);
        _preloadOp.allowSceneActivation = true;
        yield return StartCoroutine(OnActivateComplete());
    }

    IEnumerator OnActivateComplete()
    {
        // allowSceneActivation = true 후 실제 활성화까지 1프레임 대기
        yield return _preloadOp;

        Scene gachaScene = SceneManager.GetSceneByName(GACHA_SCENE);
        SceneManager.SetActiveScene(gachaScene);
        Debug.Log("[GachaLoader] 가챠씬 활성화 완료");
    }

    // ── 언로드 후 다시 Preload 대기 상태로 ────────────
    public static void Unload(Action onComplete = null)
    {
        _instance.StartCoroutine(_instance.UnloadRoutine(onComplete));
    }

    IEnumerator UnloadRoutine(Action onComplete)
    {
        _instance._isPreloaded = false;
        _instance._preloadOp = null;

        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(GACHA_SCENE);
        yield return unloadOp;

        Scene lobbyScene = SceneManager.GetSceneByName("LobbyScene");
        SceneManager.SetActiveScene(lobbyScene);

        onComplete?.Invoke();

        // 언로드 후 다시 Preload 준비
        StartCoroutine(PreloadRoutine());
        Debug.Log("[GachaLoader] 언로드 완료, 다시 Preload 시작");
    }
}