using UnityEngine;
using TMPro;
using System; // Needed for DateTime

public class DateValidator : MonoBehaviour
{
    [Tooltip("Trascina qui l'Input Field della data")]
    public TMP_InputField dateInputField;
    private const string format = "dd/MM/yyyy";

    public DateTime validatedDate { get; private set; }
    public Action OnDateSuccessfullyValidated;
    public AppLanguage currentLanguage { get; set;}

    public bool hasValidDate { get; private set; } = false;

    void Start()
    {
        if (dateInputField != null)
        {
            dateInputField.text = DateTime.Now.ToString(format);
            dateInputField.ForceLabelUpdate();
            ((TMP_Text)dateInputField.placeholder).text = currentLanguage == AppLanguage.Italian ? "Inserire data..." : "Insert date...";
            dateInputField.onEndEdit.AddListener(ValidateDate);
        }
    }

    void ValidateDate(string input)
    {
        DateTime parsedDate;

        // TryParseExact if input is compatible with format, also check if it is a valid date (e.g 30/02/20xx not valid)
        bool isValid = DateTime.TryParseExact(input, format, null, System.Globalization.DateTimeStyles.None, out parsedDate);

        if (isValid)
        {
            //Debug.Log("Data inserita correttamente: " + parsedDate.ToShortDateString());

            dateInputField.textComponent.color = Color.black;
            validatedDate = parsedDate;
            hasValidDate = true;
            OnDateSuccessfullyValidated?.Invoke();
        }
        else
        {
            //Debug.LogWarning("Formato data non valido! Usa gg/mm/aaaa");

            // UI visual error indication
            dateInputField.textComponent.color = Color.red;
            dateInputField.text = "--/--/----";
            dateInputField.caretPosition = dateInputField.text.Length; //update to avoid index error (?)

            hasValidDate = false;
        }
    }

    public DateTime GetDate()
    {
        if (hasValidDate)
        {
            return validatedDate.Date + DateTime.UtcNow.TimeOfDay;
        }
        else
        {
            return DateTime.UtcNow;
        }
    }
}