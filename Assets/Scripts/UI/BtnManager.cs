using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems; // mouse input
using UnityEngine.UI; //Image / Button
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
    [TextArea]
    public string itaTextFwdA;
    [TextArea]
    public string engTextFwdA;
    [TextArea]
    public string itaTextFwdB;
    [TextArea]
    public string engTextFwdB;

    private bool altState = false;
    private bool altSet = false;

    public UnityEvent<bool> onChangeState;
    public UnityEvent<bool> onChangeStateRev;
    public UnityEvent<string> onChangeStateStr;

    //active icons
    private Sprite iconA;
    private Sprite iconB;

    //active text
    private string itaText;
    private string engText;

    private UnityEngine.UI.Button btnComponent;

    void Start()
    {
        altSet = hasAlternativeSet;  //will be changed to false at start
        ChangeSet();
        updateFwdText(UIManager.Instance.CurrentLanguage == AppLanguage.Italian);
    }

    void Awake()
    {
        // Recuperiamo il vero e proprio componente Button attaccato a questo oggetto
        btnComponent = GetComponent<UnityEngine.UI.Button>();
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

    public void SetState(bool firstState)
    {
        if (altState != firstState)
            ChangeState();
        //else
        //    return;
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
        onChangeState.Invoke(altState);
        onChangeStateRev.Invoke(!altState);
        updateFwdText(UIManager.Instance.CurrentLanguage == AppLanguage.Italian);
    }

    public void updateFwdText(bool isIta)
    {
        
        string fwdText;
        if (isIta)
        {
            fwdText = altState ? itaTextFwdB : itaTextFwdA;
        }
        else
        {
            fwdText = altState ? engTextFwdB : engTextFwdA;
        }

        onChangeStateStr.Invoke(fwdText);
    }

    private void UpdatePanel()
    {
        bool ita = UIManager.Instance.CurrentLanguage == AppLanguage.Italian;
        tootltipText.text = ita ? itaText : engText;
    }

    public void OnPointerEnter(PointerEventData eventData) //automatic callback when mouse move INSIDE the area
    {
        if (btnComponent != null && btnComponent.interactable)
        {
            UpdatePanel();
            panel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData) //automatic callback when mouse move OUTSIDE the area
    {
        panel.SetActive(false);
    }
}