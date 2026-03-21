using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float panSpeed = 20f;         // (W, A, S, D)
    public float zoomSpeed = 10f;        // (C, X)
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
        // --- 1. PANNING (W, A, S, D) ---
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY += 1f;
        if (Input.GetKey(KeyCode.S)) moveY -= 1f;
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;
        if (Input.GetKey(KeyCode.D)) moveX += 1f;

        // Normalizing so diagonal not greater than pure x or y speed
        Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;
        transform.Translate(moveDirection * panSpeed * Time.deltaTime, Space.World);

        // --- 2. SKY ROTATION (Q, E) around Z (Vector3.forward) ---
        if (skyContainer != null)
        {
            if (Input.GetKey(KeyCode.Q)) // ACW
                skyContainer.ApplyRotation(Time.deltaTime);
            if (Input.GetKey(KeyCode.E)) // ACW
                skyContainer.ApplyRotation(-Time.deltaTime);
        }

        // --- 3. ZOOM (C, X) ---
        if (Input.GetKey(KeyCode.C))
            cam.orthographicSize -= zoomSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.X))
            cam.orthographicSize += zoomSpeed * Time.deltaTime;

        // Zoom limit
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

        // --- 4. RESET (R) ---
        if (Input.GetKeyDown(KeyCode.R))
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