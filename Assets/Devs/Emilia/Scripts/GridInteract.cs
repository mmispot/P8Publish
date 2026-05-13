using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    GridController gridController;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Pointer entered grid");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Pointer exited grid");
    }

    private void Awake()
    {
        gridController = FindFirstObjectByType(typeof(GridController)) as GridController;
    }
}
