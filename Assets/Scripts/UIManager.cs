using System.Collections; //needed for music
using TMPro; // needed for TextMeshPro!
using UnityEngine;
using UnityEngine.Video;

public enum AppLanguage
{
    Italian,
    English
}

public class UIManager : MonoBehaviour
{
    [Header("Pannelli UI")]
    public GameObject mainMenuPanel;
    public GameObject helpPanel;
    public GameObject creditPanel;

    [Header("Schermata di Avvio (WebGL)")]
    public GameObject webStartPanel;

    [Header("Lingua")]
    public AppLanguage currentLanguage = AppLanguage.Italian;

    [Header("Audio")]
    public AudioSource audioPlayer;      
    public AudioClip menuMusic;
    public AudioClip skyMusic;

    [Header("Gestione Video")]
    public VideoPlayer videoPlayer;
    public GameObject videoBackgroundUI;

    [Tooltip("Durata della dissolvenza in secondi")]
    public float fadeDuration = 3.0f;
    public float maxVolume = 0.3f; // max defaut volume, set in Unity scene the real value!

    private Coroutine currentFadeRoutine;

    [Header("Riferimenti Testi da Tradurre")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI pressAnyBtnText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI cmndsText;
    public TextMeshProUGUI escapeText;

    [Header("Conferma Uscita")]
    public GameObject quitConfirmPanel;
    public float quitConfirmTimeout = 3.0f;
    private float quitConfirmTimer = 0f;

    [Header("Riferimenti Script")]
    public CameraController cameraController;
    public StarGenerator starGenerator;
    public SkyInteractionManager skyIteractionManager;

    private bool isInMenu = true;
    private bool isHelpOpen = false;

    void Start()
    {
        SetLanguageItalian(); // Default language
#if UNITY_WEBGL
        isInMenu = false;
        cameraController.enabled = false;
        // --- WebGL intro screen to avoid audio block ---
        if (webStartPanel != null)
        {
            webStartPanel.SetActive(true);
        }
#else
        ShowMainMenu();
#endif
    }

    void Update()
    {
#if UNITY_WEBGL
        if (!isInMenu && !webStartPanel.activeSelf && !cameraController.enabled)
        {
            ShowMainMenu();
        }
#endif
        if (isInMenu && Input.anyKeyDown && 
            !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
        {
            if (!Input.GetKeyDown(KeyCode.Tab) && !Input.GetKeyDown(KeyCode.Escape))
                StartSimulation();
        }        

        // 1. ESCAPE KEY management
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isHelpOpen)
            {
                CloseHelp();
            }
            else if (creditPanel.activeSelf)
            {
                ToggleCredits();
            }
            else if (quitConfirmPanel != null && quitConfirmPanel.activeSelf)
            {
                //2nd ESC press
                QuitApplication();
            }
            else
            {
                // 1st ESC PRESS
                if (quitConfirmPanel != null)
                {
                    quitConfirmPanel.SetActive(true);
                    quitConfirmTimer = quitConfirmTimeout; // set timer
                    ToggleMenu(false);
                }
            }
        }

        // 1.5. ESC timer start
        if (quitConfirmPanel != null && quitConfirmPanel.activeSelf)
        {
            quitConfirmTimer -= Time.deltaTime; 

            if (quitConfirmTimer <= 0f)
            {
                quitConfirmPanel.SetActive(false);
                ToggleMenu(true);
            }
        }


        if (Input.GetKeyDown(KeyCode.Tab)) //Toggle Instructions (TAB)
        {
            if (!isHelpOpen)
                ShowHelp();
            else
                CloseHelp();
        }
        else if (Input.GetKeyDown(KeyCode.I)) //Toggle Light Pollution (I)
        {
            if (!isHelpOpen && !isInMenu)
            {
                if (starGenerator != null)
                {
                    starGenerator.ToggleLightPollution();
                }
            }
        }
    }

    public void StartSimulation()
    {
        isInMenu = false;
        mainMenuPanel.SetActive(false);
        helpPanel.SetActive(false);
        cameraController.enabled = true;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (videoBackgroundUI != null)
        {
            videoBackgroundUI.SetActive(false); //hide video layer
        }

        // Main phase : stars generation
        if (starGenerator != null)
        {
            starGenerator.GenerateSky();

            // Cambia la musica!
            if (audioPlayer != null && skyMusic != null)
            {
                ChangeAudioTrack(skyMusic);
            }
        }
    }

    public void ShowMainMenu()
    {
        if (webStartPanel != null && webStartPanel.activeSelf)
        {
            return; //wait for startScreenPanel to disappear
        }

        isInMenu = true;

        mainMenuPanel.SetActive(true);
        helpPanel.SetActive(false);
        cameraController.enabled = false;

        if (audioPlayer != null && menuMusic != null)
        {
            if (!audioPlayer.isPlaying)
            {
                audioPlayer.volume = 0.0f;
                ChangeAudioTrack(menuMusic);
            }
        }

        // --- Video Settings for WebGL ---
        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying)
                return; //nothing to do anymore
            // Seth dynamic path from StreamingAssets.           
            string videoName = "star_loop1.mp4";
            string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoName);
            videoPath = videoPath.Replace("\\", "/"); // avoid browser problems replacing "\\" with "/"

            videoPlayer.url = videoPath;
            videoPlayer.prepareCompleted += VideoisReady; //subscribe routine to be called at the end of Prepare

            videoPlayer.Prepare(); //launch asynch
        }
    }

    private void VideoisReady(VideoPlayer vp)
    {
        // Play when all is really ready
        vp.Play();

        if (webStartPanel != null)
        {
            webStartPanel.SetActive(false);
        }

        // unsubscribe routine
        vp.prepareCompleted -= VideoisReady;
    }

    public void JumpWebScreenPanel()
    {
        if (webStartPanel != null)
        {
#if UNITY_WEBGL
            webStartPanel.SetActive(false);
            Canvas.ForceUpdateCanvases();
#endif
        }
    }

    public void ShowHelp()
    {
        if (helpPanel == null)
            return;

        isHelpOpen = true;
        helpPanel.SetActive(true);
        cameraController.enabled = false;
    }

    public void CloseHelp()
    {
        if (helpPanel == null)
            return;

        isHelpOpen = false;
        helpPanel.SetActive(false);
        if (!isInMenu) 
            cameraController.enabled = true;
    }

    public void ToggleCredits()
    {
        if (creditPanel == null)
            return;

        TextMeshProUGUI creditText = creditPanel.GetComponentInChildren<TextMeshProUGUI>();
        if (creditText != null)
        {
            // Credit text
            string credits = "STARGATTO\n" + 
                "<size=40%>INTERACTIVE ASTRONOMIC GAME\n" +
                "Find constellation and learn about our sky\n\n" + 
                "CC BY-NC-ND 2026\n" +
                "Programming: Marco R.\n" +
                "Scientific Advisors: Dr. Elena S., Marco R.\n\n" +
                "PER AZIMUT AD ASTRA\n\n" + 
                "Menu music: \"Beyond the Gate - Inspired by Stargate\" created by Luis_Humanoide\n" +
                "Background music: ---\n";

            creditText.text = credits.ToUpper();
            creditText.alignment = TextAlignmentOptions.Left;

        }

        if (creditPanel.activeSelf)
            creditPanel.SetActive(false);
        else
            creditPanel.SetActive(true);
    }

    public void ToggleMenu(bool enable)
    {
        if (mainMenuPanel.activeSelf != enable)
        {
            mainMenuPanel.SetActive(enable);
            cameraController.enabled = !enable;
        }
    }

    // --- GAME LANGUAGES ---
    public void SetLanguageItalian()
    {
        currentLanguage = AppLanguage.Italian;

        titleText.text = "STARGAT<size=70%>TO\r\n" +
            "<size=40%>G<size=30%>IOCO <size=40%>A<size=30%>STRONOMICO PER " +
            "<size=40%>T<size=30%>ROVARE <size=40%>T<size=30%>UTTI GLI <size=40%>O<size=30%>GGETTI (DEL CIELO)!\n" +
            "<size=30%>ESPLORA. TROVA. ACCHIAPPALI TUTTI!";
        pressAnyBtnText.text = "PREMI UN TASTO PER INIZIARE";
        instructionText.text = "ISTRUZIONI";
        escapeText.text = "Premi ESC per chiudere";

        cmndsText.text =
            "W, A, S, D - Ruota la visuale\n" +
            "Q, E - Fai scorrere il tempo (Ruota il cielo)\n" +
            "C, X - Zoom Avanti e Indietro\n" +
            "I Attiva/Disattiva Inquinamento luminoso\n" +
            "R - Resetta la vista\n";

        if (skyIteractionManager != null)
            skyIteractionManager.SetCurrentLanguage(currentLanguage);
    }

    public void SetLanguageEnglish()
    {
        currentLanguage = AppLanguage.Italian;

        titleText.text = "STARGAT<size=70%>TO\r\n" +
            "<size=30%>ASTRONOMIC GAME TO FIND (ALMOST) EVERY STAR IN THE SKY\n" +
            "<size=30%>EXPLORE. FIND. GOTTA CATCH'EM ALL!";
        pressAnyBtnText.text = "PRESS ANY KEY TO START";
        instructionText.text = "INSTRUCTIONS";
        escapeText.text = "Press ESC to close";

        cmndsText.text =
            "W, A, S, D - Rotate view\n" +
            "Q, E - Pass time (Rotate sky)\n" +
            "C, X - Zoom In and Out\n" +
            "I Toggle light pollution\n" +
            "R - Reset view\n";

        if (skyIteractionManager != null)
            skyIteractionManager.SetCurrentLanguage(currentLanguage);
    }

    // AUDIO

    public void ChangeAudioTrack(AudioClip newClip)
    {
        // check if currently fading
        if (currentFadeRoutine != null)
        {
            StopCoroutine(currentFadeRoutine);
        }

        currentFadeRoutine = StartCoroutine(CrossfadeAudio(newClip));
    }

    private IEnumerator CrossfadeAudio(AudioClip newClip)
    {
        // 1. FADE OUT
        if (audioPlayer.isPlaying)
        {
            while (audioPlayer.volume > 0f)
            {
                audioPlayer.volume -= maxVolume * (Time.deltaTime / fadeDuration);

                yield return null; // Wait next frame to continue the loop!
            }
            audioPlayer.Stop();
        }

        // 2. CHANGE audio source
        audioPlayer.clip = newClip;
        audioPlayer.Play();

        // 3. FADE IN
        while (audioPlayer.volume < maxVolume)
        {
            audioPlayer.volume += maxVolume * (Time.deltaTime / fadeDuration);
            yield return null; // Wait next frame to continue the loop!
        }

        audioPlayer.volume = maxVolume; //Set exact max volume (safe)
    }

    // --- QUIT FUNCTION ---
    public void QuitApplication()
    {
        Debug.Log("Chiusura dell'applicazione...");

        // QUIT .exe file
        Application.Quit();

        // Quit game in Unity editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}