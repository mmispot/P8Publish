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
        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main.gameObject : GameObject.FindFirstObjectByType<GridController>()?.gameObject;
        gridController = mainCamera != null ? mainCamera.GetComponent<GridController>() : null;
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
