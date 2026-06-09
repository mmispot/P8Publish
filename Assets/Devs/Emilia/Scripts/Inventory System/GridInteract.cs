using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GridController gridController;
    public ItemGrid itemGrid;
    public GameObject mainCamera;

    private void Awake()
    {
        gridController = mainCamera.GetComponent<GridController>();
        itemGrid = GetComponent<ItemGrid>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"OnPointerEnter fired on: {gameObject.name}");
        gridController.selectedItemGrid = itemGrid;
        eventData.Use(); // stops the event bubbling up to parent grids
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gridController.selectedItemGrid = null;
        eventData.Use();
    }
}
