using System;
using TMPro;
using UnityEngine;

public class LocationValidator : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField latInput;
    public TMP_InputField lonInput;

    [Header("Valori Validati")]
    public float currentLatitude = 45.5f;  // Default: Costigiola
    public float currentLongitude = 14.5f;

    public Action OnLocSuccessfullyValidated;

    private void Start()
    {
        SetLatitude(currentLatitude);
        SetLongitude(currentLongitude);

        latInput.onEndEdit.AddListener(ValidateLatitude);
        lonInput.onEndEdit.AddListener(ValidateLongitude);
    }

    private void ValidateLatitude(string input)
    {
        // Prova a convertire la stringa in un numero (ignorando le lettere)
        if (float.TryParse(input, out float result))
        {
            SetLatitude(result);
        }
        //else error?
    }

    private void ValidateLongitude(string input)
    {
        if (float.TryParse(input, out float result))
        {
            SetLongitude(result);
        }
        //else error?
    }

    private void SetLatitude(float lat)
    {
        currentLatitude = Mathf.Clamp(lat, -90f, 90f);
        string hemiSphere = currentLatitude >= 0 ? "° N" : "° S";
        latInput.text = Math.Abs(currentLatitude).ToString("F2") + hemiSphere;
        OnLocSuccessfullyValidated?.Invoke();
    }

    private void SetLongitude(float lon)
    {
        currentLongitude = Mathf.Clamp(lon, -180f, 180f);
        string hemiSphere = currentLongitude >= 0 ? "° E" : "° W";
        lonInput.text = Math.Abs(currentLongitude).ToString("F2") + hemiSphere;
        OnLocSuccessfullyValidated?.Invoke();
    }

}