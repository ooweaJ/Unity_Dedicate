using Mirror;
using UnityEngine;

/// <summary>
/// 언리얼 스프링암처럼 동작하는 카메라 팔로우
/// - 플레이어 위치만 따라감 (회전은 고정)
/// - 부드러운 보간 이동
/// - 장애물 충돌 시 카메라가 앞으로 당겨짐 (스프링암 효과)
/// </summary>
public class PlayerCameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("None이면 자동으로 로컬 플레이어 찾음")]
    public Transform target;

    [Header("Camera Position")]
    public float height = 8f;       // 높이
    public float distance = 7f;     // 뒤 거리
    public float lookAtHeight = 1.5f; // 플레이어 바라볼 높이 오프셋

    [Header("Smoothing")]
    public float positionSmoothing = 8f;  // 위치 보간 속도
    public float rotationSmoothing = 5f;  // 회전 보간 속도

    [Header("Spring Arm (장애물 회피)")]
    public bool useSpringArm = true;
    public LayerMask collisionMask = ~0; // 모든 레이어

    private Vector3 currentVelocity;
    private float targetDistance;

    void Start()
    {
        targetDistance = distance;
    }

    void LateUpdate()
    {
        // 로컬 플레이어 자동 탐색
        if (target == null)
        {
            if (NetworkClient.localPlayer != null)
                target = NetworkClient.localPlayer.transform;
            else
                return;
        }

        // 카메라가 있어야 할 위치 계산 (플레이어 뒤 위쪽)
        Vector3 desiredOffset = new Vector3(0, height, -distance);
        Vector3 desiredPosition = target.position + desiredOffset;

        // 스프링암: 장애물 있으면 카메라 당기기
        if (useSpringArm)
        {
            Vector3 dir = desiredPosition - target.position;
            float checkDist = dir.magnitude;
            if (Physics.SphereCast(target.position, 0.3f, dir.normalized, out RaycastHit hit, checkDist, collisionMask))
            {
                desiredPosition = hit.point + hit.normal * 0.3f;
            }
        }

        // 위치 부드럽게 보간
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            1f / positionSmoothing
        );

        // 플레이어 바라보기 (회전 보간)
        Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
    }
}
