using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemGrid))] //geleerd van een tutorial, zorgt ervoor dat dit alles alleen werkt wanneer er ook daadwerkelijk 

public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    GridController gridController;
    ItemGrid itemGrid;

    private void Awake()
    {
        gridController = FindFirstObjectByType(typeof(GridController)) as GridController;
        itemGrid = GetComponent<ItemGrid>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        gridController.selectedItemGrid = itemGrid;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gridController.selectedItemGrid = null;
    }

}
