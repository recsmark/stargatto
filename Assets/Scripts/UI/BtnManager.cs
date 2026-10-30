using UnityEngine;
using UnityEngine.UI; //Image
using UnityEngine.EventSystems; // mouse input
using TMPro;

public class BtnManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string itaText;
    public string engText;

    [Header("riferimenti UI")]
    public GameObject panel;
    public Image imgItem;
    public TextMeshProUGUI tootltipText;

    [Header("Icone")]
    public Sprite iconA;
    public Sprite iconB;

    private bool altState = false;


    public void ChangeState()
    {
        if (imgItem == null || iconA == null || iconB == null)
        {
            CustomLogger.LogWarning("Impossibile cambiare icona: mancano dei riferimenti nell'Inspector!");
            return;
        }

        altState = !altState;

        imgItem.sprite = altState ? iconA : iconB;

    }

    public void OnPointerEnter(PointerEventData eventData) //automatic callback when mouse move INSIDE the area
    {
        bool ita = UIManager.Instance.CurrentLanguage == AppLanguage.Italian;

        tootltipText.text = ita ? itaText : engText;
        panel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) //automatic callback when mouse move OUTSIDE the area
    {
        panel.SetActive(false);
    }
}