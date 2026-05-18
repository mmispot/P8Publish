using UnityEngine;

public class GridController : MonoBehaviour
{
    public ItemGrid selectedItemGrid;

    private void Update()
    {
        if (selectedItemGrid == null) { return; }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int tileGridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);
            Debug.Log("Clicked on tile: " + tileGridPosition);
        }
    }
}
