using UnityEngine;
using UnityEngine.UI; // For UI elements
using TMPro; // For TextMeshPro UI

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // Enum für die möglichen Spielzustände
    public enum GameState
    {
        WaitingToStart, // Spiel wurde noch nicht gestartet
        Playing,        // Spiel läuft aktiv
        GameOver        // Spieler ist gestorben/Game Over
    }
    
    // Enum für die Steuerungsoptionen
    public enum ControlType
    {
        Keyboard,   // Tastatursteuerung
        Gyroscope   // Gyrosensor-Steuerung
    }
    
    [Header("Game Settings")]
    public int score = 0;
    public int highScore = 0;
    [Tooltip("Aktueller Spielzustand")]
    public GameState currentState = GameState.WaitingToStart;
    [Tooltip("Aktuelle Steuerungsmethode")]
    public ControlType currentControlType = ControlType.Keyboard;
    
    [Header("UI References")]
    [Tooltip("Ziehe dein TextMeshPro Text Element hier hinein, um den Score anzuzeigen")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public GameObject gameOverPanel;
    public Button startGameButton; // Verwende den Button im Game Over Panel
    [Tooltip("Dropdown für die Auswahl der Steuerungsmethode")]
    public TMP_Dropdown controlTypeDropdown;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Load high score from PlayerPrefs
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        
        // Load control type preference
        currentControlType = (ControlType)PlayerPrefs.GetInt("ControlType", 0);
        
        UpdateScoreUI();
    }
    
    private void Start()
    {
        // Zeige das Game Over Panel zu Beginn an (wird jetzt als Start-Panel verwendet)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Stelle sicher, dass der Start-Button einen Event-Listener hat
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
        
        // Steuerungstyp-Dropdown initialisieren
        InitializeControlTypeDropdown();
        
        // Das Spiel ist zu Beginn noch nicht gestartet
        currentState = GameState.WaitingToStart;
    }
    
    private void InitializeControlTypeDropdown()
    {
        if (controlTypeDropdown != null)
        {
            // Dropdown-Event-Listener hinzufügen
            controlTypeDropdown.onValueChanged.AddListener(OnControlTypeChanged);
            
            // Aktuellen Wert setzen
            controlTypeDropdown.value = (int)currentControlType;
        }
    }
    
    private void OnControlTypeChanged(int value)
    {
        currentControlType = (ControlType)value;
        
        // Speichern in PlayerPrefs
        PlayerPrefs.SetInt("ControlType", value);
        PlayerPrefs.Save();
        
        // SimpleMovement-Script aktualisieren
        var movementScript = FindAnyObjectByType<SimpleMovement>();
        if (movementScript != null)
        {
            movementScript.UpdateControlType(currentControlType == ControlType.Gyroscope);
        }
    }
    
    // Methode zum Starten des Spiels (wird vom Button aufgerufen)
    public void StartGame()
    {
        currentState = GameState.Playing;
        score = 0;
        UpdateScoreUI();
        
        // Verstecke das Game Over Panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        Debug.Log("Game started!");
    }
    
    // Methode zum Ändern der Steuerungsmethode von außen
    public void SetControlType(ControlType type)
    {
        currentControlType = type;
        
        if (controlTypeDropdown != null)
        {
            controlTypeDropdown.value = (int)type;
        }
        
        // Speichern in PlayerPrefs
        PlayerPrefs.SetInt("ControlType", (int)type);
        PlayerPrefs.Save();
    }
    
    public void GameOver()
    {
        currentState = GameState.GameOver;
        
        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        Debug.Log("Game Over! Final Score: " + score);
    }
    
    public void ResetGame()
    {
        // Das Spiel wird erst wieder mit dem Button gestartet
        currentState = GameState.WaitingToStart;
        score = 0;
        UpdateScoreUI();
        
        // Show game over panel with start button
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }
    
    public void AddScore(int points)
    {
        if (currentState != GameState.Playing) return;
        
        score += points;
        
        // Update high score if needed
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
        
        UpdateScoreUI();
    }
    
    private void UpdateScoreUI()
    {
        // Update score text if available
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
        
        // Update high score text if available
        if (highScoreText != null)
        {
            highScoreText.text = "High Score: " + highScore;
        }
    }
}
