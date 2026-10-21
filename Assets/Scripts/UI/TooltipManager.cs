using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    [Header("Panels")]
    public StarPanel mainPanel;
    public StarPanel secondPanel;
    private const byte fontSize = 24;

    public void enable(bool enable, byte index)
    {
        if (index == 0)
            mainPanel.gameObject.SetActive(enable);

        if (index == 1)
            secondPanel.gameObject.SetActive(enable);
    }

    public void setText(string text, byte index)
    {
        if (mainPanel != null && index == 0)
            mainPanel.label.text = text;
            
        if (secondPanel != null && index == 1)
            secondPanel.label.text = text;
    }

    public void setFontSize(bool isConstellation)
    {
        if (isConstellation)
        {
            mainPanel.label.fontSize = 36;
            return;
        }
        else
        {
#if !DEBUG
            mainPanel.label.fontSize = fontSize*0.8f; //to have _id in the name
#else
            mainPanel.label.fontSize = fontSize;
#endif
        }

        secondPanel.label.fontSize = mainPanel.label.fontSize;
    }

    private void Start()
    {
        mainPanel.label.fontSize = fontSize;
        secondPanel.label.fontSize = fontSize;
        mainPanel.gameObject.SetActive(false);
        secondPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (mainPanel != null && mainPanel.gameObject.activeInHierarchy)
        {
            mainPanel.setMainPosition();
        }

        if (secondPanel != null && secondPanel.gameObject.activeInHierarchy)
        {
            secondPanel.setSecondaryPosition();
        }
    }
}
