using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이펙트 타입 → 프리팹 매핑 테이블
/// Project 우클릭 → Create → Game/Effect Database
/// 씬에 하나만 존재하는 SO — EffectManager가 참조
/// </summary>
[CreateAssetMenu(fileName = "EffectDatabase", menuName = "Game/Effect Database")]
public class EffectDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public EffectType type;
        public GameObject prefab;
        [Tooltip("자동 소멸 시간 (초)")]
        public float      lifeTime = 2f;
    }

    public Entry[] entries;

    private Dictionary<EffectType, Entry> _map;

    public Entry Get(EffectType type)
    {
        if (_map == null) BuildMap();
        return _map.TryGetValue(type, out var e) ? e : null;
    }

    private void BuildMap()
    {
        _map = new Dictionary<EffectType, Entry>();
        foreach (var e in entries)
            _map[e.type] = e;
    }
}
