using System;
using TMPro;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public enum ProjectionType { STHEREOGRAPHIC, EQ_AZIMUTHAL }

    [Header("Impostazioni Griglia")]
    public int segments = 128;
    public Material gridMaterial;
    const float witdth = 0.1f;

    [Header("Impostazioni Testo")]
    public bool showLabels = true;
    public float fontSize = 16f;
    public float textPadding = 0.5f;

    const int baseOrder = 50;
    private bool initialstatus = false;

    void Start()
    {
#if UNITY_EDITOR
        initialstatus = false;
#endif
    }


    public void DrawGrid(float scale, float latitude, ProjectionType type)
    {
        for (int angle = 10; angle <= 90; angle += 10)
        {
            DrawCircles(angle, scale, latitude, type);
        }
        SetVisibility();
    }

    [UnityEngine.Scripting.Preserve] //prevents compilation fail of this function
    public void ToggleGrid()
    {
        initialstatus = !initialstatus;
        SetVisibility();
    }

    private void SetVisibility()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(initialstatus);
        }
    }

    public void DeleteGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    void DrawCircles(float zenitalDist, float scale, float latitude = 0f, ProjectionType type = ProjectionType.EQ_AZIMUTHAL)
    {
        GameObject circle = new GameObject($"ZDist_{zenitalDist}deg"); //declin = 90-angle
        circle.transform.SetParent(this.transform);

        LineRenderer lr = circle.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = segments + 1;
        lr.startWidth = witdth;
        lr.endWidth = witdth;
        Color lineColor = Color.white;
        //lr.startColor = Color.red;
        //lr.endColor = Color.red;
        lr.alignment = LineAlignment.TransformZ;
        if (gridMaterial != null)
        {
            lr.material = gridMaterial;
            lineColor = gridMaterial.color;
        }
        else
            lr.material = new Material(Shader.Find("Sprites/Default"));

        float radius = 0f;
        float centerY = 0f;

        if (type == ProjectionType.EQ_AZIMUTHAL)
        {
            radius = scale * (zenitalDist / 90f);
            centerY = 0f;
        }
        else if (type == ProjectionType.STHEREOGRAPHIC)
        {
            //radius = scale * Mathf.Tan((angle * Mathf.Deg2Rad) / 2f);

            float safeLat = Mathf.Max(Mathf.Abs(latitude), 0.1f);

            // North and South points of the circle of that angle
            float p_south = 90f - safeLat + zenitalDist;
            float p_north = 90f - safeLat - zenitalDist;

            // Stereographic projection (vertical line)
            float y_south = -scale * Mathf.Tan(p_south * Mathf.Deg2Rad / 2f);
            float y_north = -scale * Mathf.Tan(p_north * Mathf.Deg2Rad / 2f);

            // radius/center determined from 2 points
            radius = Mathf.Abs(y_north - y_south) / 2f;
            //tan 90°/2 = tan(45°) = 1 -> 90 ° -> scale * 1 = radius;

            // center is in the vertical line
            centerY = (y_north + y_south) / 2f;
        }

        circle.transform.localPosition = new Vector3(0, centerY, 0);
        lr.sortingOrder = baseOrder;

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * 360f / segments);
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            lr.SetPosition(i, new Vector3(x, y, -5f));
        }

        if (showLabels)
        {
            // North label
            DrawLabel(circle.transform, zenitalDist, radius + textPadding, lineColor, true);

            // South label
            DrawLabel(circle.transform, zenitalDist, -radius - textPadding, lineColor, false);
        }
    }

    private void DrawLabel(Transform parent, float angle, float posY, Color color, bool isTop)
    {
        GameObject textObj = new GameObject(isTop ? $"Label_{angle}_Top" : $"Label_{angle}_Bottom");
        textObj.transform.SetParent(parent);
        textObj.transform.localPosition = new Vector3(0, posY, -3f);

        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();

        textMesh.text = $"{angle}°";
        textMesh.fontSize = fontSize;
        textMesh.color = color; // Color.red; // color;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.sortingOrder = baseOrder+10;
    }
}