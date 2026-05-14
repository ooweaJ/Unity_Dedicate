using Mirror;
using UnityEngine;

/// <summary>
/// Mediator — IPlayerInputProvider 이벤트를 PlayerMovement / PlayerCombat에 연결
/// PC:     PlayerInputHandler (플레이어 프리팹에 부착)
/// 모바일: MobileInputProvider (씬에 배치)
/// </summary>
public class PlayerController : NetworkBehaviour
{
    private PlayerMovement       _movement;
    private PlayerCombat         _combat;
    private IPlayerInputProvider _provider;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _combat   = GetComponent<PlayerCombat>();
    }

    public override void OnStartLocalPlayer()
    {
#if UNITY_EDITOR
        bool useMobile = true;
#else
        bool useMobile = Application.isMobilePlatform;
#endif
        _provider = useMobile
            ? (IPlayerInputProvider)FindObjectOfType<MobileInputProvider>()
            : GetComponent<PlayerInputHandler>();

        if (_provider == null) return;

        _provider.OnMove          += _movement.HandleMove;
        _provider.OnJump          += _movement.HandleJump;
        _provider.OnSkillDown     += _combat.HandleSkillDown;
        _provider.OnSkillReleased += _combat.HandleSkillReleased;
    }

    private void OnDestroy()
    {
        if (_provider == null) return;
        _provider.OnMove          -= _movement.HandleMove;
        _provider.OnJump          -= _movement.HandleJump;
        _provider.OnSkillDown     -= _combat.HandleSkillDown;
        _provider.OnSkillReleased -= _combat.HandleSkillReleased;
    }
}
