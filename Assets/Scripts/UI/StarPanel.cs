using TMPro; // Necessario per TextMeshPro
using UnityEngine;

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

    public float height
    {
        get { return GetComponent<RectTransform>().rect.height; }
    }

    public Vector2 position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public void setMainPosition()
    {
        setPosition();
    }

    public void setSecondaryPosition()
    {
        setPosition(false);
    }

    public void setPosition(bool main = true) //temp main unused
    {
        RectTransform rect = GetComponent<RectTransform>();
        Vector2 mouse = Input.mousePosition;
        //Vector2 dynamicPivot = new Vector2(0.5f, offset.y > 0 ? 0f : 1f); //to manage correct positioning
        //rect.pivot = dynamicPivot;

        //adjusting for screen scale, from UI units in real pixel
        float w = width * transform.lossyScale.x;
        float h = height * transform.lossyScale.y;

        //local function (C#) instead of lambda (from C++)
        //does not allocate other memory
        void AssignPivot(ref float pivot, ref float currOffset)
        {
            if (currOffset > 0) 
                pivot = 0f;
            else if (currOffset < 0) 
                pivot = 1f;
        }

        // Base pivot from offset (Y)
        float pivotY = 0.5f;
        float currOffsetY = offset.y;
        AssignPivot(ref pivotY, ref currOffsetY);

        // Base pivot from offset (X)
        float pivotX = 0.5f;
        float currOffsetX = offset.x;
        AssignPivot(ref pivotX, ref currOffsetX);

        // calc expected corner positions, then adjust pivots to avoid overlap
        float expectedTop = mouse.y + currOffsetY + h * (1 - pivotY);
        float expectedBottom = mouse.y + currOffsetY - h * pivotY;
        float expectedRight = mouse.x + currOffsetX + w * (1 - pivotX);
        float expectedLeft = mouse.x + currOffsetX - w * pivotX;

        //local function (C#) instead of lambda (from C++)
        //does not allocate other memory
        void AdjustPivot(ref float pivot, ref float currOffset, float maxAtteso, float minAtteso, float limiteSchermo, float offsetOriginale)
        {
            if (pivot == 0.5f)
                return; //Ok

            if (pivot == 0f && maxAtteso > limiteSchermo)
            {
                pivot = 1f;
                currOffset = -Mathf.Abs(offsetOriginale);
            }
            else if (pivot == 1f && minAtteso < 0)
            {
                pivot = 0f;
                currOffset = Mathf.Abs(offsetOriginale);
            }
        }

        // Adjust pivots, then apply
        AdjustPivot(ref pivotY, ref currOffsetY, expectedTop, expectedBottom, Screen.height, offset.y);
        AdjustPivot(ref pivotX, ref currOffsetX, expectedRight, expectedLeft, Screen.width, offset.x);
        rect.pivot = new Vector2(pivotX, pivotY);

        // Clamp and lock to the border if needed
        Vector2 desiredPosition = new Vector2(mouse.x + currOffsetX, mouse.y + currOffsetY);

        float ClampPosition(float pivot, float position, float size, float screenSize)
        {
            float minPos = size * pivot;
            float maxPos = screenSize - (size * (1f - pivot));
            return Mathf.Clamp(position, minPos, maxPos);
        }

        float clampedX = ClampPosition(pivotX, desiredPosition.x, w, Screen.width);
        float clampedY = ClampPosition(pivotY, desiredPosition.y, h, Screen.height);

        transform.position = new Vector2(clampedX, clampedY);
    }
}