using System;
using UnityEngine;

public interface IPlayerInputProvider
{
    event Action<Vector2> OnMove;
    event Action          OnJump;
    event Action<int>                          OnSkillDown;
    event Action<int, Vector2, float, Vector3> OnSkillReleased;
}
