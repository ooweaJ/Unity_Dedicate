using Mirror;
using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("None이면 자동으로 로컬 플레이어 찾음")]
    public Transform target;

    [Header("Camera Position")]
    public float height = 8f;
    public float distance = 7f;
    public float lookAtHeight = 1.5f;

    [Header("Smoothing")]
    public float positionSmoothing = 8f;
    public float rotationSmoothing = 5f;

    [Header("Spring Arm")]
    public bool useSpringArm = true;
    public LayerMask collisionMask = 0; // ← ~0 에서 0으로 변경 (자기 콜라이더 히트 방지)

    private Vector3 currentVelocity;
    private bool    _cameraFlipped     = false;
    private bool    _cameraInitialized = false;

    void Start()
    {
        // SetParent(null) 제거 — 씬에 이미 있으므로 불필요
        if (target != null)
            transform.position = target.position + new Vector3(0, height, -distance);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            if (NetworkClient.localPlayer != null)
                target = NetworkClient.localPlayer.transform;
            else
                return;
        }

        // 팀 1(북쪽 진영)은 카메라를 반대 방향으로 — 1회만 판정
        if (!_cameraInitialized && BattleNetworkPlayer.Local != null)
        {
            _cameraFlipped     = BattleNetworkPlayer.Local.teamId == 1;
            _cameraInitialized = true;
        }

        float   zSign           = _cameraFlipped ? 1f : -1f;
        Vector3 desiredPosition = target.position + new Vector3(0, height, zSign * distance);

        if (useSpringArm && collisionMask != 0) // ← collisionMask 0이면 스킵
        {
            Vector3 dir = desiredPosition - target.position;
            if (Physics.SphereCast(
                    target.position, 0.3f, dir.normalized,
                    out RaycastHit hit, dir.magnitude, collisionMask))
            {
                desiredPosition = hit.point + hit.normal * 0.3f;
            }
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            1f / positionSmoothing
        );

        Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothing * Time.deltaTime
        );
    }
}