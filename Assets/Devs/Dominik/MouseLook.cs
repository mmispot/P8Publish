using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Gevoeligheid")]
    public float mouseSensitivity = 500f;

    [Header("Verticale kijk-limiet")]
    public float topClamp = -90f;
    public float bottomClamp = 90f;

    [Header("Referenties")]
    public Transform cameraTransform;
    public Transform bodyTransform; // Sleep hier de Body naartoe

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);
        yRotation += mouseX;

        // Camera kijkt omhoog/omlaag en links/rechts
        if (cameraTransform != null)
            cameraTransform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);

        // Body draait alleen op Y-as, blijft op zijn eigen positie
        if (bodyTransform != null)
            bodyTransform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    public float GetYaw()
    {
        return yRotation;
    }

    public void DisableMouseLook()
    {
        enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void EnableMouseLook()
    {
        enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}