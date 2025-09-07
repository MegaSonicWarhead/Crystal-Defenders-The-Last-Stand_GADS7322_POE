using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float sprintMultiplier = 2f;

    [Header("Mouse Settings")]
    public float lookSensitivity = 2f;
    public bool invertY = false;

    [Header("Camera Control")]
    public KeyCode lookKey = KeyCode.Mouse1; // Hold right mouse to look around

    private float pitch = 0f;
    private float yaw = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        if (Input.GetKey(lookKey))
        {
            // Hide and lock cursor while holding look key
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            pitch -= invertY ? -mouseY : mouseY;
            pitch = Mathf.Clamp(pitch, -90f, 90f);

            yaw += mouseX;

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }
        else
        {
            // Release cursor when not looking around
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        Vector3 move = (forward * Input.GetAxis("Vertical") +
                        right * Input.GetAxis("Horizontal") +
                        up * (Input.GetKey(KeyCode.E) ? 1 : 0) +
                        up * (Input.GetKey(KeyCode.Q) ? -1 : 0));

        transform.position += move * speed * Time.deltaTime;
    }
}
