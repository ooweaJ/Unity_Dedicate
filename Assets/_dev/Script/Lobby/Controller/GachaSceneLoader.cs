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

    void OnEnable()
    {
        SceneManager.sceneUnloaded += OnAnySceneUnloaded;
        SceneManager.sceneLoaded   += OnAnySceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnAnySceneUnloaded;
        SceneManager.sceneLoaded   -= OnAnySceneLoaded;
    }

    // 배틀 씬 전환 등 외부 요인으로 GachaScene이 언로드되면 상태 초기화
    private void OnAnySceneUnloaded(Scene scene)
    {
        if (scene.name == GACHA_SCENE)
        {
            _isPreloaded = false;
            _preloadOp   = null;
            Debug.Log("[GachaLoader] GachaScene 언로드 감지 → 상태 초기화");
        }
    }

    // 로비로 돌아왔을 때 preload가 사라졌으면 재시작
    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainLobbyScene" && _preloadOp == null && !_isPreloaded)
        {
            StartCoroutine(PreloadRoutine());
            Debug.Log("[GachaLoader] 로비 복귀 감지 → Preload 재시작");
        }
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
        if (_instance == null) return;

        // preloadOp가 살아있고 preload 완료 → 즉시 활성화
        if (_instance._isPreloaded && _instance._preloadOp != null)
        {
            _instance._preloadOp.allowSceneActivation = true;
            _instance.StartCoroutine(_instance.OnActivateComplete());
        }
        else
        {
            // 배틀 복귀 등으로 preload가 날아간 경우 포함 — 재로드 후 활성화
            Debug.Log("[GachaLoader] Preload 없음(또는 진행 중) → 로드 후 활성화");
            _instance.StartCoroutine(_instance.WaitAndActivate());
        }
    }

    IEnumerator WaitAndActivate()
    {
        // preload 코루틴이 시작도 안 된 경우 직접 시작
        if (_preloadOp == null && !_isPreloaded)
            StartCoroutine(PreloadRoutine());

        yield return new WaitUntil(() => _isPreloaded);
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

        Scene lobbyScene = SceneManager.GetSceneByName("MainLobbyScene");
        SceneManager.SetActiveScene(lobbyScene);

        onComplete?.Invoke();

        // 언로드 후 다시 Preload 준비
        StartCoroutine(PreloadRoutine());
        Debug.Log("[GachaLoader] 언로드 완료, 다시 Preload 시작");
    }
}