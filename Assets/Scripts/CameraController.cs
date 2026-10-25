using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float panSpeed = 20f;         // (W, A, S, D)
    public float zoomSpeed = 10f;        // (C, X)
    public float wheelSpeed = 10f;       // Mouse Wheel
    //Rotation Q, E in skyContainer

    [Header("Zoom Limits")]
    public float minZoom = 2f;
    public float maxZoom = 150f;

    [Header("References")]
    public StarContainer skyContainer;

    private Camera cam;

    // Reset vars
    private Vector3 initialPosition;
    private float initialZoom;

    private Vector3 dragOrigin = new Vector3(0f, 0f, 0f);

    private float initialRotationH;

    void Start()
    {
        cam = GetComponent<Camera>();

        //2D set
        cam.orthographic = true;
        transform.position = new Vector3(0f, 0f, -10f);

        if (skyContainer != null)
        {
            InitSpace();
        }
    }

    void Update()
    {
        //CustomLogger.Log("Pointer on UI: " + EventSystem.current.IsPointerOverGameObject());
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // ignore inputs
            return; //TBD
        }

        //save wheel scroll
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        // --- 1. PANNING (W, A, S, D) ---
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY += 1f;
        if (Input.GetKey(KeyCode.S)) moveY -= 1f;
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;
        if (Input.GetKey(KeyCode.D)) moveX += 1f;

        // Normalizing so diagonal not greater than pure x or y speed
        Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;

        if (Input.GetMouseButtonDown((int)CmdMapper.MouseButton.MOUSE_WHEEL_CLICK))
        {
            // Save cursor position when click of wheel occurs
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // Added panning with mouse wheel click
        if (Input.GetMouseButton((int)CmdMapper.MouseButton.MOUSE_WHEEL_CLICK))
        {
            // move with -= to simulate the "grab" of the mouse
            // 1:1 matched with the screen
            Vector3 currentMousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            moveDirection.x = dragOrigin.x - currentMousePos.x;
            moveDirection.y = dragOrigin.y - currentMousePos.y;

            // instead of a fixed scale
            //moveDirection.x -= Input.GetAxis("Mouse X") * panSpeed;
            //moveDirection.y -= Input.GetAxis("Mouse Y") * panSpeed;
        }

        if (moveDirection.sqrMagnitude > 0)
        {
            transform.Translate(moveDirection * panSpeed * Time.deltaTime, Space.World);
        }
        // --- 2. SKY ROTATION (Q, E) around Z (Vector3.forward) ---
        //adding else, do not get rotation when translation active)
        else if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E))
        {
            if (skyContainer != null)
            {
                if (Input.GetKey(KeyCode.Q)) // ACW
                    skyContainer.ApplyRotation(Time.deltaTime);
                if (Input.GetKey(KeyCode.E)) // ACW
                    skyContainer.ApplyRotation(-Time.deltaTime);
            }
        }
        else if (scrollInput != 0f || // ---3.ZOOM(Mouse Wheel or keyboard)-- -
                 Input.GetKey(KeyCode.C) ||
                 Input.GetKey(KeyCode.X))
        { 
            if (scrollInput != 0f)
            {
                cam.orthographicSize -= scrollInput * wheelSpeed; // -= : wheel fwd = zoom in
            }
            else if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.X))
            {
                // --- 3.ZOOM(C, X) ---
                if (Input.GetKey(KeyCode.C))
                    cam.orthographicSize -= zoomSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.X))
                    cam.orthographicSize += zoomSpeed * Time.deltaTime;
            }

            // Zoom limit
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
        // --- 4. RESET (R) ---
        else if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }
    }

    void ResetCamera()
    {
        transform.position = initialPosition;
        cam.orthographicSize = initialZoom;

        if (skyContainer != null)
        {
            skyContainer.ApplyReset();
        }
    }

    void InitSpace()
    {
        // Save state for reset (R)
        initialPosition = transform.position;
        initialZoom = cam.orthographicSize;

        skyContainer.Initialize();
    }
}