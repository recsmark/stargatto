using UnityEngine;
using TMPro; // TextMeshPro
using System; //dateTime
using System.Globalization;

public class SkyInteractionManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public StarGenerator starGenerator;
    public StarContainer skyContainer;
    public Camera mainCamera;

    [Header("GameUI")]
    public TextMeshProUGUI upperText;
    public TextMeshProUGUI bottomText;

    [Header("Tooltip UI")] // tooltip (Star/Constellation name)
    public RectTransform tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public Vector2 tooltipOffset = new Vector2(15f, -15f); // Cursor offset

    private string currentHoveredConstellation = "";
    private AppLanguage currentLanguage = AppLanguage.Italian; //see UIManager

    void Update()
    {
        // 1. Reset tooltip every frame
        tooltipPanel.gameObject.SetActive(false);
        string newHoveredConstellation = "";

        // 2. Convert mouse position
        Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // 3. Calculat the hit with 2D "ray"
        Collider2D hit = Physics2D.OverlapPoint(mousePosition);

        if (hit != null)
        {
            string objName = hit.gameObject.name;
            //Debug.Log($"[RAYCAST] Ho toccato: {objName}");

            if (objName.StartsWith("Star_"))
            {
                // Get star ID from name (es: from "Star_123" to "123") ...
                int starId = int.Parse(objName.Split('_')[1]);

                // ... and use in dictionary to retrieve name
                if (starGenerator.database.starDict.ContainsKey(starId))
                {
                    string starName = starGenerator.database.starDict[starId].name;
#if DEBUG           
                    //add id to catch quick an draw missing lines in development
                    starName += "_" + starId.ToString();
#endif
                    if (!string.IsNullOrEmpty(starName))
                    {
                        ShowTooltip(starName);
                    }
                }
            }
            else if (objName.StartsWith("Line_"))
            {
                // Get constellation ID from name (es: from "Line_Ori" to "Ori") ..
                string constId = objName.Split('_')[1];

                // search only if already visible
                if (starGenerator.skyRevealed || starGenerator.visibleConstellations.Contains(constId))
                {
                    newHoveredConstellation = constId;
                    // ... and use in dictionary to retrieve name
                    if (starGenerator.database.constellationDict.ContainsKey(constId))
                    {
                        string fullName = currentLanguage == AppLanguage.Italian ? starGenerator.database.constellationDict[constId].itaName
                             : starGenerator.database.constellationDict[constId].itaName;
                        ShowTooltip(fullName, true);
                    }
                }

                // Discover lines by click
                if (Input.GetMouseButtonDown(0)) //left click
                {
                    if (!starGenerator.visibleConstellations.Contains(constId))
                    {
                        starGenerator.RevealConstellation(constId);
                    }

                    // Call level 2 details (TODO)
                    OpenDetailsPanel(constId, "Constellation");
                }
            }
        }

        // --- GLOW when HOVER on constellation ---

        // Remove glow if needed
        if (currentHoveredConstellation != "" && currentHoveredConstellation != newHoveredConstellation)
        {
            starGenerator.SetConstellationGlow(currentHoveredConstellation, false);
        }

        // Apply glow if constellation hit
        if (newHoveredConstellation != "" && newHoveredConstellation != currentHoveredConstellation)
        {
            starGenerator.SetConstellationGlow(newHoveredConstellation, true);
        }

        // Trace the current constellation
        currentHoveredConstellation = newHoveredConstellation;

        updateGameUI();
    }

    // --- UTILITY METHODS ---
    private void ShowTooltip(string textToShow, bool isConstellation = false)
    {
        tooltipPanel.gameObject.SetActive(true);
        if (isConstellation)
        {

        }
        tooltipText.text = textToShow;

        // 1. Tooltip absolute position
        Vector2 desiredPosition = (Vector2)Input.mousePosition + tooltipOffset;

        float width = tooltipPanel.rect.width;
        float height = tooltipPanel.rect.height;

        // 3. SCREEN CLAMPING, then apply position
        float clampedX = Mathf.Clamp(desiredPosition.x, width, Screen.width);
        float clampedY = Mathf.Clamp(desiredPosition.y, height, Screen.height);

        tooltipPanel.position = new Vector2(clampedX, clampedY);
    }

    // Detail panel function TODO
    public void OpenDetailsPanel(string targetId, string targetType)
    {
        // targetType can be "Star" or "Constellation"
        Debug.Log($"[PLACEHOLDER] Apertura pannello dettagli per: {targetType} con ID: {targetId}");

        // TODO: more detailed panels
    }

    public void SetCurrentLanguage(AppLanguage language)
    {
        currentLanguage = language;
    }

    public void updateGameUI()
    {
        float rotationH = skyContainer.SkyRotation; //gives delta between current and UTC (+ sky rotation)
        DateTime currentTime = System.DateTime.UtcNow.AddHours(rotationH); //current time = UTC + rotation
        upperText.text = currentTime.ToString();
        if (currentLanguage == AppLanguage.Italian)
            upperText.text += " " + TimeZoneInfo.Local.StandardName; //can't translate
        upperText.text += " @ Long. " + skyContainer.localLongitude.ToString() + "°"; 
        
        /*ParseExact(
                               "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                               CultureInfo.InvariantCulture.DateTimeFormat,
                               DateTimeStyles.AssumeUniversal);*/

        if (currentLanguage == AppLanguage.Italian)
            bottomText.text = "Costellazioni trovate ";
        else
            bottomText.text = "Found constellations ";

        bottomText.text += starGenerator.visibleConstellations.Count.ToString() + "/" + starGenerator.totalConstellations.ToString();

        if (currentLanguage == AppLanguage.Italian)
            bottomText.text += " - Stelle: ";
        else
            bottomText.text += " - Stars :";

        bottomText.text += starGenerator.totalStars.ToString();

        if (currentLanguage == AppLanguage.Italian)
            bottomText.text += " - ISTRUZIONI: ";
        else
            bottomText.text += " - INSTRUCTIONS: ";
        bottomText.text += " TAB";
    }
}