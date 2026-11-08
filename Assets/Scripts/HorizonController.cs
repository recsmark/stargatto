using UnityEngine;
using TMPro;

public class HorizonController : MonoBehaviour
{
    private float equatorRadius = 1f;

    [Header("Test in Tempo Reale")]
    [Range(0.1f, 89.9f)]
    public float testLatitude = 45f;

    [Header("Grafica Orizzonte")]
    public LineRenderer horizonLine;
    public int lineSegments = 64; 

    [Header("Punti Cardinali")]
    public Transform northLabel;
    public Transform southLabel;
    public Transform eastLabel;
    public Transform westLabel;

    private float textPadding = 4;

    public float EquatorRadius { get => equatorRadius; set => equatorRadius = value; }

    void Update()
    {
        //UpdateHorizonMask(testLatitude);
    }

    public void UpdateHorizonMask(float latitude, bool relativeProj)
    {
        float safeLat = Mathf.Max(Mathf.Abs(latitude), 0.1f);
        float latRad = safeLat * Mathf.Deg2Rad;

        float radius = equatorRadius / Mathf.Sin(latRad);
        float yOffset = equatorRadius / Mathf.Tan(latRad);
        if (latitude < 0) 
            yOffset = -yOffset;

        float centerY = relativeProj ? 0 : -yOffset;

        // Move visible part
        transform.localPosition = new Vector3(0, centerY, 0);
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        // Draw line
        if (horizonLine != null)
        {
            DrawHorizonLine(radius, centerY);
        }

        // Set labels
        PositionCardinalPoints(radius, centerY);
    }

    private void DrawHorizonLine(float radius, float centerY)
    {
        // Draw circle by segments
        horizonLine.positionCount = lineSegments + 1;

        for (int i = 0; i <= lineSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / lineSegments;
            float x = Mathf.Cos(angle) * radius;
            float y = centerY + Mathf.Sin(angle) * radius;

            horizonLine.SetPosition(i, new Vector3(x, y, 0));
        }
#if UNITY_EDITOR
        //Draw reference center
        Vector2 centerPos = new Vector2(0f, centerY);
        DrawCenterRef(centerPos);
#endif
    }

    private void PositionCardinalPoints(float radius, float centerY)
    {

        if (northLabel != null) 
            northLabel.position = new Vector3(0, centerY + radius + textPadding, 0);
        if (southLabel != null) 
            southLabel.position = new Vector3(0, centerY - radius - textPadding, 0);

        // Eeast-west swap!
        if (eastLabel != null) 
            eastLabel.position = new Vector3(-radius - textPadding, centerY, 0);
        if (westLabel != null) 
            westLabel.position = new Vector3( radius + textPadding, centerY, 0);
    }

#if UNITY_EDITOR
    // Red X
    private void DrawCenterRef(Vector2 pos, float size = 5f, float duration = 2f)
    {
        Vector3 topDx = new Vector3(pos.x + size, pos.y + size, 0);
        Vector3 lowSx = new Vector3(pos.x - size, pos.y - size, 0);

        Vector3 topSx = new Vector3(pos.x - size, pos.y + size, 0);
        Vector3 lowDx = new Vector3(pos.x + size, pos.y - size, 0);

        Debug.DrawLine(lowSx, topDx, Color.red, duration);
        Debug.DrawLine(topSx, lowDx, Color.red, duration);
    }
#endif
}