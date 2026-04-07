using Mirror;
using UnityEngine;

/// <summary>
/// 애니메이션 동기화 전담 컴포넌트
/// 언리얼 RepNotify = Mirror SyncVar + hook
/// 몽타주 = Animator Layer (Base / Action) + Avatar Mask
/// </summary>
public class PlayerAnimationController : NetworkBehaviour
{
    // ─── 파라미터 해시 (string 비교 제거, 성능 최적화) ───────────────────
    private static readonly int ParamSpeed = Animator.StringToHash("Speed");
    private static readonly int ParamDirection = Animator.StringToHash("Direction");
    private static readonly int ParamJumpHeight = Animator.StringToHash("JumpHeight");
    private static readonly int ParamGravityControl = Animator.StringToHash("GravityControl");
    private static readonly int ParamJump = Animator.StringToHash("Jump");
    private static readonly int ParamRest = Animator.StringToHash("Rest");
    // 몽타주 레이어 트리거 (Layer 1: Action Layer)
    private static readonly int ParamAttack = Animator.StringToHash("Attack");
    private static readonly int ParamSkill = Animator.StringToHash("Skill");
    private static readonly int ParamHit = Animator.StringToHash("Hit");

    private Animator animator;
    private CharacterSpawner characterSpawner;

    // ─── SyncVar : 이동 파라미터 ─────────────────────────────────────────
    // 언리얼 RepNotify와 동일 — 서버에서 값 변경 시 모든 클라이언트 hook 호출
    [SyncVar(hook = nameof(OnSyncSpeed))]
    private float syncSpeed;

    [SyncVar(hook = nameof(OnSyncDirection))]
    private float moveDirectionSync;

    [SyncVar(hook = nameof(OnSyncJumpHeight))]
    private float syncJumpHeight;

    [SyncVar(hook = nameof(OnSyncGravityControl))]
    private float syncGravityControl;

    [SyncVar(hook = nameof(OnSyncJump))]
    private bool syncJump;

    [SyncVar(hook = nameof(OnSyncRest))]
    private bool syncRest;

    // ─── Hook : SyncVar 변경 시 자동 호출 ────────────────────────────────
    // (언리얼 UFUNCTION() + RepNotify 조합과 동일)
    private void OnSyncSpeed(float _, float v) => animator.SetFloat(ParamSpeed, v);
    private void OnSyncDirection(float _, float v) => animator.SetFloat(ParamDirection, v);
    private void OnSyncJumpHeight(float _, float v) => animator.SetFloat(ParamJumpHeight, v);
    private void OnSyncGravityControl(float _, float v) => animator.SetFloat(ParamGravityControl, v);
    private void OnSyncJump(bool _, bool v) => animator.SetBool(ParamJump, v);
    private void OnSyncRest(bool _, bool v) => animator.SetBool(ParamRest, v);

    private void Awake()
    {
        characterSpawner = gameObject.GetComponent<CharacterSpawner>();
    }
    private void OnEnable()
    {
        if (characterSpawner != null)
        {
            characterSpawner.OnCharacterSpawned += SetAnimater;
        }
    }
    private void OnDisable()
    {
        if (characterSpawner != null)
        {
            characterSpawner.OnCharacterSpawned -= SetAnimater;
        }
    }

    public void SetAnimater(Animator animator)
    {
        this.animator = animator;
    }

    // ─── 로컬 플레이어 파라미터 업데이트 (PlayerController에서 호출) ──────
    // Command로 서버에 전달 → SyncVar 변경 → Hook → 모든 클라이언트 반영
    public void UpdateMovementParams(float speed, float direction,
                                     float jumpHeight, float gravityControl,
                                     bool jump, bool rest)
    {
        // isLocalPlayer 대신 localMode도 같이 체크
        if (!isLocalPlayer && !GetComponent<PlayerController>().localMode) return;

        // 로컬 애니메이터 직접 세팅 (서버 불필요)
        animator.SetFloat(ParamSpeed, speed);
        animator.SetFloat(ParamDirection, direction);

        animator.SetBool(ParamJump, jump);
        animator.SetBool(ParamRest, rest);

        // 서버 연결됐을 때만 Command 전송
        if (isLocalPlayer)
            CmdSetMovementParams(speed, direction, jumpHeight, gravityControl, jump, rest);
    }

    [Command]
    private void CmdSetMovementParams(float speed, float direction,
                                       float jumpHeight, float gravity,
                                       bool jump, bool rest)
    {
        syncSpeed = speed;
        moveDirectionSync = direction;
        syncJumpHeight = jumpHeight;
        syncGravityControl = gravity;
        syncJump = jump;
        syncRest = rest;
    }

    // ─── 몽타주 계열: 공격 / 스킬 트리거 ────────────────────────────────
    // 언리얼 PlayMontage() → Mirror [ClientRpc] SetTrigger()
    // PlayerCombat에서 서버 판정 후 아래 함수들 호출

    [ClientRpc]
    public void RpcPlayAttack()
    {
        Debug.Log("Rpc어택");
        animator.SetTrigger(ParamAttack);
    }

    [ClientRpc]
    public void RpcPlaySkill()
    {
        animator.SetTrigger(ParamSkill);
    }

    [ClientRpc]
    public void RpcPlayHit()
    {
        animator.SetTrigger(ParamHit);
    }

    // ─── 로컬 플레이어 선입력 (레이턴시 보상) ────────────────────────────
    // 공격 입력 시 로컬에서 즉시 애니 재생, 서버 응답으로 다른 클라이언트 반영
    public void PlayAttackLocal() => animator.SetTrigger(ParamAttack);
    public void PlaySkillLocal() => animator.SetTrigger(ParamSkill);
}