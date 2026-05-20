using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    public float bobAmplitude = 0.02f; 
    public float bobFrequency = 0.5f;  

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    { 
        float newY = startPos.y + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}

