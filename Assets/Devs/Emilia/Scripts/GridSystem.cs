using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GridSystem : MonoBehaviour
{
    //TUTORIAL USED: https://youtu.be/ur1TeqxFtV4?si=Tv8Fm4t4xDZQ5gSf
    public GameObject objectToPlace;
    public float gridSize = 1f;
    private GameObject ghostObject;
    private HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

    void Start()
    {
        CreateGhostObject();
    }

    void Update()
    {
        UpdateGhostPosition();
        if (Input.GetMouseButtonDown(0))
        {
            PlaceObject();
        }
    }

    void CreateGhostObject()
    {
        ghostObject = Instantiate(objectToPlace);
        ghostObject.GetComponent<Collider>().enabled = false;

        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material material = new Material(renderer.material);
            material.color = new Color(material.color.r, material.color.g, material.color.b, 0.5f);
            renderer.material = material;

            material.SetFloat("_Mode", 2);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
    }

    void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 point = hit.point;

            Vector3 snappedPosition = new Vector3(
                Mathf.Round(point.x / gridSize) * gridSize,
                Mathf.Round(point.y / gridSize) * gridSize,
                Mathf.Round(point.z / gridSize) * gridSize
            );

            ghostObject.transform.position = snappedPosition;

            if (occupiedPositions.Contains(snappedPosition))
            {
                SetGhostColor(Color.red);
            } else
            {
                SetGhostColor(new Color(1f, 1f, 1f, 0.5f));
            }

        }
    }

    void SetGhostColor(Color color)
        {
            Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material material = renderer.material;
                material.color = new Color(color.r, color.g, color.b, 0.5f);
            }
        }

    void PlaceObject()
    {
        Vector3 placementPosition = ghostObject.transform.position;

        if (!occupiedPositions.Contains(placementPosition))
        {
            Instantiate(objectToPlace, placementPosition, Quaternion.identity);
            occupiedPositions.Add(placementPosition);
        }
    }
}
