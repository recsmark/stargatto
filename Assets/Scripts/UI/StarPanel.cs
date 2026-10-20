using UnityEngine;
using TMPro; // Necessario per TextMeshPro

public class StarPanel : MonoBehaviour
{
    [Header("Properties")]
    [Tooltip("Cursor offset")]
    public Vector2 offset;

    [Tooltip("TooltipText")]
    public TextMeshProUGUI label;

    public float width
    {
        get { return GetComponent<RectTransform>().rect.width; }
    }

    public float heigth
    {
        get { return GetComponent<RectTransform>().rect.height; }
    }

    public Vector2 position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public void setPosition()
    {
        // 1. Tooltip absolute position
        Vector2 desiredPosition = (Vector2)Input.mousePosition + offset;

        // 3. SCREEN CLAMPING, then apply position
        float clampedX = Mathf.Clamp(desiredPosition.x, width, Screen.width);
        float clampedY = Mathf.Clamp(desiredPosition.y, heigth, Screen.height);

        transform.position = new Vector2(clampedX, clampedY);
    }
}