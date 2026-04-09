using System.Collections.Generic;
using UnityEngine;

public class CharacterModelManager : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    private Dictionary<int, GameObject> _pool = new();
    private int _currentId = -1;

    public void ShowModel(CharacterUIModel model)
    {
        int targetId = model.StaticData.id;
        if (_currentId == targetId) return;

        // 1. 기존 캐릭터 끄기 (있을 때만)
        if (_currentId != -1 && _pool.TryGetValue(_currentId, out var oldModel))
        {
            oldModel.SetActive(false);
        }

        // 2. 새 캐릭터 가져오기
        if (!_pool.TryGetValue(targetId, out var newModel))
        {
            newModel = Instantiate(model.StaticData.modelPrefab, spawnPoint);
            _pool.Add(targetId, newModel);
        }

        newModel.SetActive(true);
        _currentId = targetId;
    }

    public void ClearAllModels()
    {
        foreach (var obj in _pool.Values) Destroy(obj);
        _pool.Clear();
        _currentId = -1;
    }
}