using UnityEngine;

public class GridController : MonoBehaviour
{
    [SerializeField] ItemGrid selectedItemGrid;

    private void Update()
    {
        if (selectedItemGrid == null) { return; }
    }
}
