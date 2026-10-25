using System; //dateTime
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using TMPro; // TextMeshPro
using UnityEngine;

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
    public TooltipManager tooltip;
    //public TextMeshProUGUI tooltipText;
    //public Vector2 tooltipOffset = new Vector2(15f, -15f); // Cursor offset
    
    private const float PC_TO_LYEAR = 3.26156f; //convert parsec to light years

    public DateValidator validator;

    private string currentHoveredConstellation = "";
    private AppLanguage currentLanguage = AppLanguage.Italian; //see UIManager

    void Update()
    {
        // 1. Reset tooltip every frame
        tooltip.enable(false, 0);
        tooltip.enable(false, 1);
        string newHoveredConstellation = "";

        // 2. Convert mouse position
        Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // 3. Calculat the hit with 2D "ray"
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);
        //Collider2D oldHit = Physics2D.OverlapPoint(mousePosition);
        Collider2D priorityHit = null;

        if (hits.Length > 0)
        {
            // gives priority to star colliders (CircleCollider2D)
            foreach (Collider2D col in hits)
            {
                if (col is CircleCollider2D)
                {
                    priorityHit = col;
                    break;
                }
            }

            // take the first hit
            if (priorityHit == null)
            {
                priorityHit = hits[0];
            }
        }
        //else if (oldHit != null)
        //{
        //    priorityHit = oldHit;
        //}

        if (priorityHit != null)
        {
            string objName = priorityHit.gameObject.name;
            //CustomLogger.Log($"[RAYCAST] Ho toccato: {objName}");

            if (objName.StartsWith("Star_"))
            {
                // Get star ID from name (es: from "Star_123" to "123") ...
                uint starId = uint.Parse(objName.Split('_')[1]);

                // ... and use in dictionary to retrieve name
                if (starGenerator.database.starDict.ContainsKey(starId))
                {
                    StarData star = starGenerator.database.starDict[starId];
                    if (star.mainStar)
                    {
                        ShowStarTooltip(ref star);
                    }

                    if (star.binCount == 1)
                    {
                        //2nd star
                        StarData second = starGenerator.database.starDict[star.idsBins[0]];
                        if (second != null)
                            Show2ndStarTooltip(ref second, star.name);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD //development build only when builds
                        //add line name
                        fullName = objName;
#endif
                        ShowTooltip(fullName, true);
                    }
                    else
                    {
                        CustomLogger.Log($"[DB] costellazione mancante: {constId}");
                    }
                }

                // Discover lines by left click
                if (Input.GetMouseButtonDown((int)CmdMapper.MouseButton.MOUSE_LEFT))
                {
                    if (!starGenerator.visibleConstellations.Contains(constId))
                    {
                        starGenerator.RevealConstellation(constId);
                    }

                    // Call level 2 details (TODO)
                    OpenDetailsPanel(constId, "Constellation");
                }
                //hide lines by right click
                else if (Input.GetMouseButtonDown((int)CmdMapper.MouseButton.MOUSE_RIGHT))
                {
                    if (starGenerator.visibleConstellations.Contains(constId))
                    {
                        starGenerator.HideConstellation(constId);
                    }
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

        UpdateGameUI();
    }

    // --- UTILITY METHODS ---
    private void ShowStarTooltip(ref StarData star, bool isMain = true, bool sameName = false)
    {
        static string AssignLetter(bool isMain, byte binCount)
        {
            return isMain ? (binCount > 0 ? "A" : "") : "B";
        }

        string starName = star.name;
        if (string.IsNullOrEmpty(star.name))
            starName = AssignLetter(isMain, star.binCount); //overwrite null string
        else
        {
            if (sameName) //tag each star
                starName += " " + AssignLetter(isMain, star.binCount);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD //development build only when builds
        //add id to catch quick an draw missing lines in development
        starName += "_" + star.hr.ToString(); //use hr to faster check lines 
#endif
        string textToShow = starName + "\nm " + star.mag.ToString("0.00") + "\n" + (star.dist * PC_TO_LYEAR).ToString("0.00");

        if (currentLanguage == AppLanguage.Italian)
            textToShow += " a.l.";
        else
            textToShow += " ly";

        ShowTooltip(textToShow, false, !isMain);
    }

    private void Show2ndStarTooltip(ref StarData star, string mainName)
    {
        ShowStarTooltip(ref star, false, mainName == star.name);
    }

    private void ShowTooltip(string textToShow, bool isConstellation = false, bool isBinary = false)
    {
        byte index = isBinary ? (byte)1 : (byte)0;
        tooltip.setFontSize(isConstellation);
        tooltip.setText(textToShow, index);
        tooltip.enable(true, index);
    }

    // Detail panel function TODO
    public void OpenDetailsPanel(string targetId, string targetType)
    {
        // targetType can be "Star" or "Constellation"
        CustomLogger.Log($"[PLACEHOLDER] Apertura pannello dettagli per: {targetType} con ID: {targetId}");

        // TODO: more detailed panels
    }

    public void SetCurrentLanguage(AppLanguage language)
    {
        currentLanguage = language;
        validator.currentLanguage = language;
    }

    public void UpdateGameUI()
    {
        float rotationH = skyContainer.SkyRotationUTC; //gives delta between current and UTC (+ sky rotation)
        DateTime currentDate = validator?.GetDate() ?? DateTime.UtcNow;
        //using AddHours make the object a TimeSpan, so the new format is needed
        upperText.text = currentDate.AddHours(rotationH).TimeOfDay.ToString(@"hh\:mm\:ss"); //current SIDEREAL time = UTC + rotation
        if (currentLanguage == AppLanguage.Italian)
            upperText.text += " UTC / Cielo posizionato @ Long. ";
        else
            upperText.text += " UTC / Sky positioned @ Long. ";

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