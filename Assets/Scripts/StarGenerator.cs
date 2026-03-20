using UnityEngine;
using System.Collections.Generic;
using System.Linq; // needed for search

public class StarGenerator : MonoBehaviour
{
    [Header("Riferimenti Script")]
    public StarDatabase database;

    [Header("Prefabs & Containers")]
    public GameObject starPrefab;
    public Transform starContainer;

    [Header("Impostazioni Proiezione 2D")]
    public float projectionScale = 50f;

    [Header("Impostazioni Linee e Interazioni")]
    public float lineWidth = 0.1f;
    public Material lineMaterial;
    
    // I tre stati di colore per le linee
    public Color hiddenColor = new Color(0.2f, 0.6f, 1.0f, 0f);   // Alpha 0 = Invisible
    public Color normalColor = new Color(0.2f, 0.6f, 1.0f, 0.4f); // Normal
    public Color glowColor = new Color(0.8f, 0.9f, 1.0f, 0.9f);   // Glowing

    [Header("Inquinamento Luminoso")]
    public Camera mainCamera;
    public Color normalSkyColor = new Color(0.01f, 0.02f, 0.05f);  // Normal sky color
    public Color pollutedSkyColor = new Color(0.1f, 0.15f, 0.25f); // Polluted City sky color
    public float pollutionThreshold = 3f; // Stelle con magnitudine >

    private bool isPolluted = false;
    private bool reveal = false;

    // Stars
    private Dictionary<int, GameObject> instantiatedStars = new Dictionary<int, GameObject>();

    // Every lines of a constellation
    private Dictionary<string, List<LineRenderer>> constellationLines = new Dictionary<string, List<LineRenderer>>();

    // Discovered constellations
    public HashSet<string> visibleConstellations = new HashSet<string>();
    public int totalConstellations;
    public int totalStars;

    // Colors
    private class ColorTable
    {
        public struct bvColor
        {
            public float bv;
            public Color color;

            public bvColor(float bvInd, int red, int green, int blue, int alpha = 255)
            {
                bv = bvInd;
                color = new Color(red/255f, green/255f, blue/255f, alpha/255f);
            }

            public bvColor(float bvInd, float red, float green, float blue, float alpha = 1f)
            {
                bv = bvInd;
                color = new Color(red, green, blue, alpha);
            }
        }

        public bvColor[] wikiBvColors = new bvColor[]
        {
            //WIKIPEDIA COLOR TABLE
            //from BV Index like https://en.wikipedia.org/wiki/Color_index
            new bvColor(-0.33f, 148, 182, 255),
            new bvColor(-0.30f, 153, 185, 255),
            new bvColor(-0.02f, 201, 217, 255),
            new bvColor( 0.30f, 236, 238, 255),
            new bvColor( 0.58f, 255, 242, 238),
            new bvColor( 0.81f, 255, 231, 210),
            new bvColor( 1.40f, 255, 204, 152)
        };

        // Color Stellarium have poor reds ...
        // https://www.celestialprogramming.com/articles/starColors/ColorStellarium.js

        //another color table by AI
        public bvColor[] aiBvColors = new bvColor[]
        {
            new bvColor( -0.2f, 0.6f, 0.7f, 1.0f),
            new bvColor(  0.1f, 0.8f, 0.9f, 1.0f),
            new bvColor(  0.5f, 1.0f, 1.0f, 1.0f),
            new bvColor(  0.9f, 1.0f, 1.0f, 0.8f),
            new bvColor(  1.4f, 1.0f, 0.8f, 0.4f),
            new bvColor(  2.0f, 1.0f, 0.5f, 0.5f)
        };

        public Color bvToColor(float bvIndex, bvColor[] colorTable)
        {
            //DECIDE which colortable to use
            for (int ind = 0; ind < colorTable.Length; ind++)
            {
                if (colorTable[ind].bv >= bvIndex)
                    return colorTable[ind].color;
            }
            //safe exit
            return colorTable.Last().color;
        }
    }

    private bool skyGenerated = false;
    private ColorTable myPalette = new ColorTable();

    private float minStarRadius = 0.1f; //default min value
    private float maxStarRadius = 1.0f; //default max value

    private float shrinkMargin = 1f; //for not overlap star and lines collider

    public bool skyRevealed => reveal;

    void Start()
    {
        // wait for DB
        if (database == null)
        {
            Debug.LogError("StarDatabase non collegato o non caricato!");
        }
    }

    public void GenerateSky() //callback for UI
    {
        if (skyGenerated) return; // generate once

        LoadStars();
        DrawConstellations();

        skyGenerated = true;
    }

    void LoadStars()
    {
        foreach (var kvp in database.starDict)
        {
            StarData star = kvp.Value;
            GenerateProjectedStar(star.id, star.name, star.ra, star.dec, star.colorIndex, star.mag);
        }
        Debug.Log($"Generazione completata: {instantiatedStars.Count} stelle create a schermo.");

        totalConstellations = database.constellationDict.Count;
        totalStars = database.starDict.Count;
    }

    void GenerateProjectedStar(int starId, string starName, float raHours, float decDegrees, float bvIndex, float magnitude)
    {
        float raRad = raHours * 15f * Mathf.Deg2Rad;
        float decRad = decDegrees * Mathf.Deg2Rad;

        float x = Mathf.Cos(decRad) * Mathf.Cos(raRad);
        float y = Mathf.Cos(decRad) * Mathf.Sin(raRad);
        float z = Mathf.Sin(decRad);

        float denominator = 1.0f + z;
        if (denominator < 0.001f) return;

        float projectedX = x / denominator;
        float projectedY = y / denominator;

        Vector3 finalPosition = new Vector3(projectedX * projectionScale, projectedY * projectionScale, 0f);

        GameObject newStar = Instantiate(starPrefab, starContainer);
        // localPosition should follow future rotations
        newStar.transform.localPosition = finalPosition;

        float starScale = CalculateStarScale(magnitude);
        newStar.transform.localScale = new Vector3(starScale, starScale, 1f);

        newStar.name = $"Star_{starId}"; 

        //Renderer and collider properties
        SpriteRenderer starRenderer = newStar.GetComponent<SpriteRenderer>();
        CircleCollider2D col = newStar.AddComponent<CircleCollider2D>();
        if (starRenderer != null)
        {
            starRenderer.sortingOrder = 1; //the greater the nearer
            starRenderer.color = CalculateStarColor(bvIndex);
            col.radius = starRenderer.bounds.extents.x;  //CalculateStarRadius(magnitude, starRenderer);
        }
       
        instantiatedStars.Add(starId, newStar);
    }

    void DrawConstellations()
    {
        foreach (var link in database.linkList)
        {
            if (instantiatedStars.ContainsKey(link.startId) && instantiatedStars.ContainsKey(link.endId))
            {
                Vector3 startLocalPos = instantiatedStars[link.startId].transform.localPosition;
                Vector3 endLocalPos = instantiatedStars[link.endId].transform.localPosition;

                // Star radius needed to shrink lines
                float startRadius = instantiatedStars[link.startId].GetComponent<SpriteRenderer>().bounds.extents.x;
                float endRadius = instantiatedStars[link.endId].GetComponent<SpriteRenderer>().bounds.extents.x;

                CreateConstellationLine(startLocalPos, endLocalPos, link.constellationId,
                                        startRadius, endRadius, 
                                        link.startId, link.endId);
            }
        }
    }

    void CreateConstellationLine(Vector3 startLocalPos, Vector3 endLocalPos, string constId, 
                                 float startRadius, float endRadius,
                                 int startId, int endId)
    {
        // Formatted name, unique name since includes star id "Line_@const_star1_star2"
        GameObject lineObj = new GameObject($"Line_{constId}_{startId}_{endId}");
        lineObj.transform.SetParent(starContainer);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.sortingOrder = -1; //the greater the nearer

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        if (lineMaterial != null) lr.sharedMaterial = lineMaterial;
        
        //Lines start not visible
        lr.startColor = hiddenColor;
        lr.endColor = hiddenColor;
        lr.useWorldSpace = false; 

        EdgeCollider2D edgeCol = lineObj.AddComponent<EdgeCollider2D>();

        //Short line collider to gave star the priority
        Vector3 direction = (endLocalPos - startLocalPos);
        float shrink = Mathf.Min(direction.magnitude * 0.25f, shrinkMargin);
        Vector3 versor = direction.normalized;

        Vector3 shortenedStart = startLocalPos + (versor * shrink);
        Vector3 shortenedEnd = endLocalPos - (versor * shrink);
        lr.SetPosition(0, shortenedStart);
        lr.SetPosition(1, shortenedEnd);

        edgeCol.points = new Vector2[] { shortenedStart, shortenedEnd };
        edgeCol.edgeRadius = lineWidth * 1.0f; // click distance tolerance, TBD

        // SAVE CREATED LINE
        if (!constellationLines.ContainsKey(constId))
        {
            constellationLines.Add(constId, new List<LineRenderer>());
        }
        constellationLines[constId].Add(lr);
    }

    // Publick callbacks
    public void RevealConstellation(string constId)
    {
        if (constellationLines.ContainsKey(constId))
        {
            visibleConstellations.Add(constId); // Marked as visible
            SetConstellationGlow(constId, true); // First glow
        }
    }

    public void ToggleLightPollution()
    {
        if (!skyGenerated) return;

        isPolluted = !isPolluted;

        // Change background
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = isPolluted ? pollutedSkyColor : normalSkyColor;
        }

        // Filtering on magnitude
        foreach (var kvp in instantiatedStars)
        {
            int starId = kvp.Key;
            float mag = database.starDict[starId].mag;

            // the greater, the less bright
            if (mag > pollutionThreshold)
            {
                // disable stars and relative colliders
                kvp.Value.SetActive(!isPolluted);
            }
        }

        // Hide "orphan" lines
        foreach (var link in database.linkList)
        {
            float startMag = database.starDict[link.startId].mag;
            float endMag = database.starDict[link.endId].mag;

            // check if each point should be visible
            if (startMag > pollutionThreshold || endMag > pollutionThreshold)
            {
                // Search by its unique name
                Transform lineTransform = starContainer.Find($"Line_{link.constellationId}_{link.startId}_{link.endId}");
                if (lineTransform != null)
                {
                    lineTransform.gameObject.SetActive(!isPolluted);
                }
            }
        }

        Debug.Log($"Inquinamento luminoso: {(isPolluted ? "ATTIVO" : "DISATTIVO")}");
    }


    public void ToggleReveal()
    {
        reveal = !skyRevealed;
        foreach (var constId in database.constellationDict.Keys)
        {
            if (constellationLines.ContainsKey(constId))
            {
                Color targetColor = skyRevealed ? normalColor : hiddenColor;
                if (visibleConstellations.Contains(constId))
                    targetColor = normalColor;
                foreach (LineRenderer lr in constellationLines[constId])
                {
                    lr.startColor = targetColor;
                    lr.endColor = targetColor;
                }
            }
        }

    }

    public void SetConstellationGlow(string constId, bool isGlowing)
    {
        if (constellationLines.ContainsKey(constId) && (visibleConstellations.Contains(constId) || skyRevealed))
        {
            Color targetColor = isGlowing ? glowColor : normalColor;
            
            foreach (LineRenderer lr in constellationLines[constId])
            {
                lr.startColor = targetColor;
                lr.endColor = targetColor;
            }
        }
    }

    // --- UTILITY ---
    Color CalculateStarColor(float bvIndex)
    {
        //choose wich color table with the 2nd argument
        return myPalette.bvToColor(bvIndex, myPalette.wikiBvColors);
    }

    float CalculateStarScale(float magnitude)
    {
        // Apparent Magnitude -1.5 (Sirio, brighter) to 6.5 (naked eye)
        // InverseLerp interpolates these  value from 1 to 0, reversed
        // (es: mag 6.0 -> 0.0, mag -1.5 -> 1.0)
        float normalizedMag = Mathf.InverseLerp(database.maxNakedEyeMagnitude, -1.5f, magnitude);

        // map normalization to visible scales

        float finalScale = Mathf.Lerp(minStarRadius, maxStarRadius, normalizedMag);
        
        return finalScale;
    }
}


