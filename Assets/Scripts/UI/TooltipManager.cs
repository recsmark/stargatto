using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    [Header("Panels")]
    public StarPanel mainPanel;
    public StarPanel secondPanel;

    public void enable(bool enable1, bool enable2 = false)
    {
        mainPanel.gameObject.SetActive(enable1);
        secondPanel.gameObject.SetActive(enable2);
    }

    public void setText(string text1, string text2 = "")
    {
        if (mainPanel != null)
            mainPanel.label.text = text1;

        if (secondPanel != null && text2 != "")
            secondPanel.label.text = text2;
    }

    public void setFontSize(bool isConstellation)
    {
        if (isConstellation)
        {
            mainPanel.label.fontSize = 36;
        }
        else
        {
            mainPanel.label.fontSize = 24;
        }
    }

    void Update()
    {
        if (mainPanel != null && mainPanel.gameObject.activeInHierarchy)
        {
            mainPanel.setPosition();
        }

        if (secondPanel != null && secondPanel.gameObject.activeInHierarchy)
        {
            secondPanel.setPosition();
        }
    }
}
