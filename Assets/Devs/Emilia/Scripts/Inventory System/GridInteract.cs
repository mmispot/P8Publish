using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemGrid))] //geleerd van een tutorial, zorgt ervoor dat dit alles alleen werkt wanneer er ook daadwerkelijk een ItemGrid is

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
        gridController.selectedItemGrid = itemGrid;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gridController.selectedItemGrid = null;
    }

}
