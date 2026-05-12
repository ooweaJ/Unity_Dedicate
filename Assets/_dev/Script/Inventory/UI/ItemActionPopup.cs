using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class ItemActionPopup : MonoBehaviour
{
    [SerializeField] private Button    btnPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private Vector2   offset = new Vector2(75, -100);

    private readonly List<Button> _buttons = new();

    public void Show(ItemRawData data, Vector2 position, Action<ItemActionType> onAction)
    {
        foreach (var b in _buttons)
            if (b != null) Destroy(b.gameObject);
        _buttons.Clear();

        var actions = GetActionsForType(data.itemType);
        if (actions.Count == 0) { Hide(); return; }

        foreach (var action in actions)
        {
            var btn = Instantiate(btnPrefab, container);
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = action.label;

            var capturedType = action.type;
            btn.onClick.AddListener(() => { onAction?.Invoke(capturedType); Hide(); });
            _buttons.Add(btn);
        }

        transform.position = position + offset;
        gameObject.SetActive(true);
    }

    // 장비 전용 오버로드 (강화 + 장착)
    public void ShowEquipment(Vector2 position, Action<ItemActionType> onAction)
    {
        foreach (var b in _buttons)
            if (b != null) Destroy(b.gameObject);
        _buttons.Clear();

        var actions = new List<ItemActionDef>
        {
            new() { label = "강화", type = ItemActionType.Enhance },
            new() { label = "장착", type = ItemActionType.Equip   }
        };

        foreach (var action in actions)
        {
            var btn = Instantiate(btnPrefab, container);
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = action.label;

            var capturedType = action.type;
            btn.onClick.AddListener(() => { onAction?.Invoke(capturedType); Hide(); });
            _buttons.Add(btn);
        }

        transform.position = position + offset;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    private static List<ItemActionDef> GetActionsForType(ItemType type) => type switch
    {
        ItemType.Consumable => new List<ItemActionDef>
        {
            new() { label = "사용",   type = ItemActionType.Use     },
            new() { label = "버리기", type = ItemActionType.Discard }
        },
        _ => new List<ItemActionDef>()
    };
}
