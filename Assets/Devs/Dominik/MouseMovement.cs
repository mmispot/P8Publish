//Tutorial that I used: https://youtu.be/swOfmyJvb98?si=FnuUb9Xatvsdfstt

using UnityEngine;

public class MouseMovement : MonoBehaviour
{

    public float mouseSensitivity = 500f;

    float xRotation = 0f;
    float yRotation = 0f;

    public float topClamp = -90f;
    public float bottomClamp = 90f;

    void Start()
    {
        //Locks the cursor in the middle of the screen 
        Cursor.lockState = CursorLockMode.Locked;


    }

    void Update()
    {
        //Getting mouse inputs
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        //Rotate around the x axis (up & down)
        xRotation -= mouseY;

        //Clamp rotation
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);

        //Rotate around the x axis (left & right)
        yRotation += mouseX;

        //Apply rotations to transform
        transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

    }
}
