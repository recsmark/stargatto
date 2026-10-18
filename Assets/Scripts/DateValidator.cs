using UnityEngine;
using TMPro;
using System; // Necessario per usare la classe DateTime

public class DateValidator : MonoBehaviour
{
    [Tooltip("Trascina qui l'Input Field della data")]
    public TMP_InputField dateInputField;
    private const string format = "dd/MM/yyyy";

    // Aggiungiamo questa variabile pubblica per memorizzare la data valida
    public DateTime validatedDate { get; private set; }
    public Action OnDateSuccessfullyValidated;
    public AppLanguage currentLanguage { get; set;}

    // Questa variabile ci dice se c'è una data valida pronta all'uso
    public bool hasValidDate { get; private set; } = false;

    void Start()
    {
        // Ci assicuriamo che l'Input Field sia collegato
        if (dateInputField != null)
        {
            // Aggiungiamo un comando che scatta appena l'utente finisce di scrivere
            dateInputField.text = DateTime.Now.ToString(format);
            ((TMP_Text)dateInputField.placeholder).text = currentLanguage == AppLanguage.Italian ? "Inserire data..." : "Insert date...";
            dateInputField.onEndEdit.AddListener(ValidateDate);
        }
    }

    void ValidateDate(string input)
    {
        // Definiamo il formato esatto richiesto (MM maiuscolo indica i mesi in C#)
        DateTime parsedDate;

        // TryParseExact controlla se la stringa inserita è una data vera nel formato specificato
        // Inoltre, riconosce in automatico che "30/02/2024" è invalido, o gli anni bisestili.
        bool isValid = DateTime.TryParseExact(input, format, null, System.Globalization.DateTimeStyles.None, out parsedDate);

        if (isValid)
        {
            //Debug.Log("Data inserita correttamente: " + parsedDate.ToShortDateString());

            // Imposta il colore del testo su nero (o bianco, a seconda del tuo tema) per confermare
            dateInputField.textComponent.color = Color.black;

            validatedDate = parsedDate;
            hasValidDate = true;
            OnDateSuccessfullyValidated?.Invoke();
        }
        else
        {
            //Debug.LogWarning("Formato data non valido! Usa gg/mm/aaaa");

            // Colora il testo di rosso per far capire all'utente che c'è un errore
            dateInputField.textComponent.color = Color.red;

            // OPZIONALE: Se vuoi che il campo si svuoti quando la data è sbagliata, de-commenta la riga sotto:
            dateInputField.text = "--/--/----"; 

            hasValidDate = false;
        }
    }

    public DateTime GetDate()
    {
        if (hasValidDate)
        {
            return validatedDate;
        }
        else
        {
            return DateTime.UtcNow;
        }
    }
}