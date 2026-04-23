using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 아이콘/수량 표시 등 슬롯의 기본 시각적 요소만 담당하는 추상 클래스
/// </summary>
public abstract class BaseSlot : MonoBehaviour
{
    [SerializeField] protected Image imgIcon;
    [SerializeField] protected TextMeshProUGUI txtAmount;
    [SerializeField] protected GameObject emptyIndicator;

    public int ItemId { get; private set; } = -1;
    public ItemRawData StaticData { get; private set; }
    public bool IsEmpty => ItemId == -1;

    public virtual void SetItem(int itemId, ItemRawData data, int amount)
    {
        ItemId = itemId;
        StaticData = data;
        
        if (imgIcon != null)
        {
            imgIcon.sprite = data.icon;
            imgIcon.color = Color.white;
            imgIcon.gameObject.SetActive(true);
        }
        
        if (txtAmount != null)
        {
            txtAmount.text = amount > 1 ? amount.ToString() : "";
        }
        
        if (emptyIndicator != null)
            emptyIndicator.SetActive(false);
    }

    public virtual void Clear()
    {
        ItemId = -1;
        StaticData = null;
        
        if (imgIcon != null)
            imgIcon.gameObject.SetActive(false);
            
        if (txtAmount != null)
            txtAmount.text = "";
            
        if (emptyIndicator != null)
            emptyIndicator.SetActive(true);
    }
}
