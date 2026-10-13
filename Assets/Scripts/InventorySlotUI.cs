using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 背包格子的鼠标悬停行为：进入时请 UIManager 弹出该道具的效果说明，离开时收起。
/// 挂在格子自身的 GameObject 上（该对象必须有一张 raycastTarget = true 的 Image）。
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemKind kind;

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance?.ShowItemTooltip(kind, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance?.HideItemTooltip();
    }
}
