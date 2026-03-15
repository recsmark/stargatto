using UnityEngine;

public class CameraController3D : MonoBehaviour
{
    [Header("Impostazioni Velocità")]
    public float panSpeed = 20f;         // (W, A, S, D)
    public float zoomSpeed = 10f;        // (C, X)
    public float skyRotationSpeed = 15f; // (Q, E)

    [Header("Limiti Zoom (FOV)")]
    public float minFOV = 5f;
    public float maxFOV = 90f;

    [Header("Riferimenti")]
    public Transform skyContainer;

    private Camera cam;

    // Reset vars
    private Quaternion initialCamRotation;
    private float initialFOV;
    private Quaternion initialSkyRotation;
    private bool stateSaved = false;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Save state for reset (R)
        initialCamRotation = transform.rotation;
        initialFOV = cam.fieldOfView;
    }

    void Update()
    {
        // Save state in first frame, so StarGeneration already occurred (TBD 3D latitude set)
        if (!stateSaved && skyContainer != null)
        {
            initialSkyRotation = skyContainer.rotation;
            stateSaved = true;
        }

        // --- Head Pitch (W, S) ---
        if (Input.GetKey(KeyCode.W))
            transform.Rotate(Vector3.right, -skyRotationSpeed * Time.deltaTime, Space.Self);
        if (Input.GetKey(KeyCode.S))
            transform.Rotate(Vector3.right, skyRotationSpeed * Time.deltaTime, Space.Self);

        // --- Head Yaw (A, D) ---
        if (Input.GetKey(KeyCode.A))
            transform.Rotate(Vector3.up, -skyRotationSpeed * Time.deltaTime, Space.World);
        if (Input.GetKey(KeyCode.D))
            transform.Rotate(Vector3.up, skyRotationSpeed * Time.deltaTime, Space.World);

        // --- Sky rotation (Q, E) ---
        if (skyContainer != null)
        {
            // Nota: Usiamo Space.Self. rotates sky around "its" Z vector, wich is Polaris
            if (Input.GetKey(KeyCode.Q)) // ACW
                skyContainer.Rotate(Vector3.up, skyRotationSpeed * Time.deltaTime, Space.Self);
            if (Input.GetKey(KeyCode.E)) // CW
                skyContainer.Rotate(Vector3.up, -skyRotationSpeed * Time.deltaTime, Space.Self);
        }

        // --- ZOOM (C, X) - Field of View ---
        if (Input.GetKey(KeyCode.C))
            cam.fieldOfView -= zoomSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.X))
            cam.fieldOfView += zoomSpeed * Time.deltaTime;

        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFOV, maxFOV);

        // --- 5. RESET (R) ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.rotation = initialCamRotation;
            cam.fieldOfView = initialFOV;

            if (skyContainer != null)
            {
                skyContainer.rotation = initialSkyRotation;
            }
        }
    }
}