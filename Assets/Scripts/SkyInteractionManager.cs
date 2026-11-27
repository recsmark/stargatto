using System; //dateTime
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using TMPro; // TextMeshPro
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SkyInteractionManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public StarGenerator starGenerator;
    public StarContainer skyContainer;
    public Camera mainCamera;

    [Header("GameUI")]
    public TextMeshProUGUI UTC_Text;
    public TextMeshProUGUI LST_Text;
    public TextMeshProUGUI bottomText;

    [Header("Tooltip UI")] // tooltip (Star/Constellation name)
    public TooltipManager tooltip;
    public DateValidator validator;
    //public TextMeshProUGUI tooltipText;
    //public Vector2 tooltipOffset = new Vector2(15f, -15f); // Cursor offset

    [Header("Actions")]
    public UnityEvent<string> onChangeLang;
    public UnityEvent<bool> onChangeLangIsIta;

    private const float PC_TO_LYEAR = 3.26156f; //convert parsec to light years

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

        // 3. Calculate the hit with 2D "ray"
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);
        //Collider2D oldHit = Physics2D.OverlapPoint(mousePosition);
        Collider2D priorityHit = null;

        if (hits.Length > 0)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // ignore inputs, prevents discovering constellation under bars
                return;
            }

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

            if (objName.StartsWith("Star_") || objName.StartsWith("Planet_"))
            {
                // Get star ID from name (es: from "Star_123" to "123") ...
                uint starId = uint.Parse(objName.Split('_')[1]);

                // ... and use in dictionary to retrieve name
                if (starGenerator.database.pointDict.ContainsKey(starId))
                {
                    SkyObject point = starGenerator.database.pointDict[starId];
                    switch (point)
                    {
                        case Star star:
                            if (star.mainStar)
                            {
                                ShowStarTooltip(ref star, star.binCount);
                            }

                            if (star.binCount == 1)
                            {
                                //2nd star
                                Star star2 = starGenerator.database.pointDict[star.idsBins[0]] as Star;
                                if (star2 != null)
                                    Show2ndStarTooltip(ref star2, star2.binCount, star.name);
                            }
                            break;
                        case Planet planet:
                            ShowPlanetTooltip(ref planet);
                            break;
                        default:
                            break;
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
    private void ShowStarTooltip(ref Star obj, byte count, bool isMain = true, bool sameName = false)
    {
        static string AssignLetter(bool isMain, byte binCount)
        {
            return isMain ? (binCount > 0 ? "A" : "") : "B";
        }

        string pointName = obj.name;
        if (string.IsNullOrEmpty(obj.name))
            pointName = AssignLetter(isMain, count); //overwrite null string
        else
        {
            if (sameName) //tag each star
                pointName += " " + AssignLetter(isMain, count);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD //development build only when builds
        //add id to catch quick an draw missing lines in development
        Star ss = obj as Star;
        if (ss != null)
        {
            pointName += "_" + ss.hr.ToString(); //use hr to faster check lines 
        }
#endif
        string textToShow = pointName + "\nm " + obj.mag.ToString("0.00") + "\n" + (obj.dist * PC_TO_LYEAR).ToString("0.00");

        if (currentLanguage == AppLanguage.Italian)
            textToShow += " a.l.";
        else
            textToShow += " ly";

        ShowTooltip(textToShow, false, !isMain);
    }

    private void ShowPlanetTooltip(ref Planet obj)
    {
        string pointName = obj.name;
        string textToShow = pointName + "\nm " + obj.mag.ToString("0.00") + "\n" + (obj.dist).ToString("0.00");

        if (currentLanguage == AppLanguage.Italian)
            textToShow += " U.A.";
        else
            textToShow += " AU";

        ShowTooltip(textToShow, false, false);
    }

    private void Show2ndStarTooltip(ref Star star, byte count, string mainName)
    {
        ShowStarTooltip(ref star, count, false, mainName == star.name);
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
        //beomed useless here
        //float rotationH = skyContainer.SkyRotationUTC; //gives delta between current and UTC (+ sky rotation)

        DateTime currentDate = validator?.GetDate() ?? DateTime.UtcNow;
        //using AddHours make the object a TimeSpan, so the new format is needed
        //UTC_Text.text = currentDate.AddHours(rotationH).TimeOfDay.ToString(@"hh\:mm\:ss") + " UTC";
        UTC_Text.text = currentDate.TimeOfDay.ToString(@"hh\:mm\:ss") + " UTC";

#if UNITY_EDITOR
        LST_Text.text = "LST: " + TimeSpan.FromHours(skyContainer.SkyRotationLST).ToString(@"hh\:mm\:ss");
        float degRotation = skyContainer.SkyRotationUTC * 15f;
        //LST_Text.text = "ROT " + degRotation.ToString("F2") + "°";
#else
        LST_Text.gameObject.SetActive(false);
#endif

        //if (currentLanguage == AppLanguage.Italian)
        //    upperText.text += " UTC";
        //else
        //    upperText.text += " UTC";

        /*ParseExact(
                               "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                               CultureInfo.InvariantCulture.DateTimeFormat,
                               DateTimeStyles.AssumeUniversal);*/
        string bottomText;
        bool ita = currentLanguage == AppLanguage.Italian;

        if (ita)
            bottomText = "Costellazioni trovate ";
        else
            bottomText = "Found constellations ";

        bottomText += starGenerator.visibleConstellations.Count.ToString() + "/" + starGenerator.totalConstellations.ToString();

        if (ita)
            bottomText += " - Stelle: ";
        else
            bottomText += " - Stars :";

        //bottomText += starGenerator.totalStars.ToString();
        bottomText += starGenerator.totalVisiblelStars.ToString();

        if (ita)
            bottomText += "\nISTRUZIONI: ";
        else
            bottomText += "\nINSTRUCTIONS: ";
        bottomText += " TAB";


        onChangeLang.Invoke(bottomText);
        onChangeLangIsIta.Invoke(ita);
    }
}