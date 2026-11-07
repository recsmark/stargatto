using TMPro;
using UnityEngine;
using UnityEngine.EventSystems; // mouse input
using UnityEngine.UI; //Image
using UnityEngine.UIElements;

//
//Update the sky with the new position

public class BtnManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    [Header("riferimenti UI")]
    public GameObject panel;
    public UnityEngine.UI.Image imgItem;
    public TextMeshProUGUI tootltipText;

    [Header("Ammetti 2° set")]
    public bool hasAlternativeSet;

    [Header("Icone")]
    public Sprite iconStateASet1;
    public Sprite iconStateBSet1;
    public Sprite iconStateASet2;
    public Sprite iconStateBSet2;

    [Header("Testi")]
    [TextArea] //allows newline
    public string itaTextStateA;
    [TextArea]
    public string engTextStateA;
    [TextArea]
    public string itaTextStateB;
    [TextArea]
    public string engTextStateB;

    private bool altState = true;  //will be changed to false at start
    private bool altSet = false;

    //active icons
    private Sprite iconA;
    private Sprite iconB;

    //active text
    private string itaText;
    private string engText;

    void Start()
    {
        altSet = hasAlternativeSet;  //will be changed to false at start
        ChangeSet();
        ChangeState();
    }

    public void ChangeSet()
    {
        if (hasAlternativeSet)
        {
            altSet = !altSet;
        }
        //else maintain false, only one set

        iconA = altSet ? iconStateASet2 : iconStateASet1;
        iconB = altSet ? iconStateBSet2 : iconStateBSet1;
        UpdateIcon();
    }

    void UpdateIcon()
    {
        imgItem.sprite = altState ? iconB : iconA; //update Icon

        itaText = altState ? itaTextStateB : itaTextStateA;
        engText = altState ? engTextStateB : engTextStateA;

        UpdatePanel();
    }

    public void ChangeState()
    {
        if (imgItem == null || iconA == null || iconB == null)
        {
            CustomLogger.LogWarning("Impossibile cambiare icona: mancano dei riferimenti nell'Inspector!");
            return;
        }

        altState = !altState;
        UpdateIcon();
    }

    void UpdatePanel()
    {
        bool ita = UIManager.Instance.CurrentLanguage == AppLanguage.Italian;
        tootltipText.text = ita ? itaText : engText;
    }

    public void OnPointerEnter(PointerEventData eventData) //automatic callback when mouse move INSIDE the area
    {
        UpdatePanel();
        panel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) //automatic callback when mouse move OUTSIDE the area
    {
        panel.SetActive(false);
    }
}