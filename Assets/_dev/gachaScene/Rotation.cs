using UnityEngine;

public class Rotation : MonoBehaviour
{
    [Header("회전 설정")]
    [Tooltip("초당 회전 속도입니다.")]
    public float rotationSpeed = 20f;

    [Tooltip("회전할 축을 선택하세요.")]
    public Vector3 rotationAxis = Vector3.up; // 기본값은 Y축 (좌우 회전)

    void Update()
    {
        // Time.deltaTime을 곱해줘야 프레임 드랍이 생겨도 일정한 속도로 회전합니다.
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}
