using System.Collections.Generic;

public enum SkillInputType
{
    Directional,  // 조이스틱 방향으로 발사
    TargetPoint,  // 조이스틱 방향+크기로 착탄점 결정 (수류탄류)
}

[System.Serializable]
public class SkillData
{
    public string            skillName    = "스킬";
    public float             cooldown     = 0.8f;
    public float             lockDuration = 0.5f; // 스킬 사용 후 다음 입력 차단 시간
    public SkillInputType    inputType    = SkillInputType.Directional;
    public List<SkillAction> actions      = new List<SkillAction>();
}
