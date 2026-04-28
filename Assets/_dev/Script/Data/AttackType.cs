/// <summary>
/// 공격 타입 — AttackData에서 사용
/// Melee:      OverlapSphere 근접 판정
/// Projectile: 투사체 Spawn
/// Dash:       지정 방향 돌진 + 종점 피해
/// </summary>
public enum AttackType
{
    Melee,
    Projectile,
    Dash
}
