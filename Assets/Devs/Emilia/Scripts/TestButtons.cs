using UnityEngine;

public class TestButtons : MonoBehaviour
{

    public GameObject testItem;

    public GridSystem gridSystem;

    private void Start()
    {
        gridSystem = GameObject.FindWithTag("GridSystem").GetComponent<GridSystem>();
        Debug.Log("GridSystem found: " + (gridSystem != null));
    }

    public void ChangeItemPrefab()
    {
        //gridSystem.ghostObject = testItem;
        gridSystem.objectToPlace = testItem;
    }
}
