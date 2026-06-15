using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 0.15f;

    [Header("Vertical clamp")]
    [SerializeField] private float topClamp = -90f;
    [SerializeField] private float bottomClamp = 90f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform bodyTransform;

    private float _xRotation;
    private float _yRotation;

    private void Start()
    {
        //lock and hide the cursor on start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _yRotation = transform.eulerAngles.y;
    }

    private void Update()
    {
        // read raw per-frame mouse delta — never an absolute position, never needs Time.deltaTime
        Vector2 delta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;

        _xRotation -= delta.y * mouseSensitivity;
        _xRotation = Mathf.Clamp(_xRotation, topClamp, bottomClamp);
        _yRotation += delta.x * mouseSensitivity;

        //rotate the camera on both axes
        if (cameraTransform != null)
            cameraTransform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);

        //rotate the body on the y axis only
        if (bodyTransform != null)
            bodyTransform.rotation = Quaternion.Euler(0f, _yRotation, 0f);
    }

    public float GetYaw()
    {
        //return the current yaw so other scripts can align to the camera direction
        return _yRotation;
    }

    public void DisableMouseLook()
    {
        //disable this script and unlock the cursor
        enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void EnableMouseLook()
    {
        //enable this script and lock the cursor again
        enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}