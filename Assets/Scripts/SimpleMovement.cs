using UnityEngine;
using UnityEngine.InputSystem;
// Füge einen Alias für den Gyroscope-Typ aus dem neuen Input System hinzu
using InputSystemGyroscope = UnityEngine.InputSystem.Gyroscope;

public class SimpleMovement : MonoBehaviour
{
    [Tooltip("Rotationsgeschwindigkeit für Links/Rechts-Drehung")]
    public float horizontalRotationSpeed = 120f;
    
    [Tooltip("Rotationsgeschwindigkeit für Hoch/Runter-Drehung")]
    public float verticalRotationSpeed = 120f;

    [Tooltip("Konstante Vorwärtsgeschwindigkeit")]
    public float forwardSpeed = 3f;

    [Tooltip("Aktiviere/Deaktiviere Bewegung auf der Y-Achse (Hoch/Runter)")]
    public bool allowVerticalMovement = true;
    
    [Tooltip("Invertiere die Hoch/Runter-Steuerung")]
    public bool invertVerticalControl = false;
    
    [Tooltip("Aktiviere/Deaktiviere Gyrosensor-Steuerung für Mobilgeräte")]
    public bool useGyroscope = true;
    
    [Tooltip("Empfindlichkeit der Gyrosensor-Steuerung")]
    public float gyroSensitivity = 2.5f;
    
    [Tooltip("Invertiere die Gyro-Steuerung links/rechts")]
    public bool invertGyroHorizontal = false;
    
    [Tooltip("Invertiere die Gyro-Steuerung oben/unten")]
    public bool invertGyroVertical = false;

    private InputAction moveAction;
    private Vector3 moveDirection;
    
    // Gyroscope Actions (neues Input System)
    private InputAction gyroAttitudeAction;
    private InputAction gyroRotationRateAction;
    private bool gyroAvailable = false;
    private Quaternion initialGyroAttitude;
    private Quaternion calibrationQuaternion;

    private void Awake()
    {
        // Neues InputAction-Objekt vom Typ Value (liefert Vector2)
        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );

        // Composite für WASD + Pfeiltasten
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/s")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/a")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");

        moveAction.Enable();

        // Gyroscope actions einrichten
        gyroAttitudeAction = new InputAction(
            name: "GyroAttitude",
            type: InputActionType.Value,
            expectedControlType: "Quaternion"
        );
        gyroAttitudeAction.AddBinding("<Gyroscope>/attitude");
        
        gyroRotationRateAction = new InputAction(
            name: "GyroRotationRate",
            type: InputActionType.Value,
            expectedControlType: "Vector3"
        );
        gyroRotationRateAction.AddBinding("<Gyroscope>/angularVelocity");
        
        // Initiale Bewegungsrichtung ist vorwärts (lokale Z-Achse)
        moveDirection = transform.forward;
        
        // Check GameManager Control Type setting
        CheckControlTypeSettings();
    }
    
    private void CheckControlTypeSettings()
    {
        // Wenn GameManager existiert, verwende dessen Steuerungseinstellung
        if (GameManager.Instance != null)
        {
            useGyroscope = GameManager.Instance.currentControlType == GameManager.ControlType.Gyroscope;
        }
        else
        {
            // Standard: Tastatursteuerung für Desktop, Gyro für Mobile
            #if UNITY_WEBGL
            // Im WebGL-Build standardmäßig Tastatursteuerung verwenden
            useGyroscope = false;
            #endif
        }
        
        // Gyrosensor initialisieren, falls benötigt
        if (useGyroscope)
        {
            InitializeGyroscope();
        }
    }
    
    // Öffentliche Methode zum Aktualisieren der Steuerungsmethode
    public void UpdateControlType(bool useGyro)
    {
        if (useGyroscope == useGyro)
            return; // Keine Änderung nötig
        
        useGyroscope = useGyro;
        
        if (useGyroscope)
        {
            // Gyro aktivieren/initialisieren
            InitializeGyroscope();
        }
        else
        {
            // Gyro deaktivieren
            DisableGyroscope();
        }
        
        Debug.Log("Steuerungsmethode geändert: " + (useGyroscope ? "Gyrosensor" : "Tastatur"));
    }
    
    private void DisableGyroscope()
    {
        if (gyroAvailable)
        {
            gyroAttitudeAction.Disable();
            gyroRotationRateAction.Disable();
            gyroAvailable = false;
        }
    }
    
    private void InitializeGyroscope()
    {
        // Prüfen, ob Gyrosensor verfügbar ist
        if (useGyroscope && Accelerometer.current != null && InputSystemGyroscope.current != null)
        {
            gyroAvailable = true;
            
            // Gyro-Actions aktivieren
            gyroAttitudeAction.Enable();
            gyroRotationRateAction.Enable();
            
            // Ein bisschen Zeit geben, um ersten Gyro-Werte zu bekommen
            StartCoroutine(CalibrateGyroscopeAfterDelay(0.2f));
            
            Debug.Log("Gyrosensor initialisiert");
        }
        else if (useGyroscope)
        {
            Debug.Log("Gyrosensor ist auf diesem Gerät nicht verfügbar");
            useGyroscope = false;
            
            // Wenn Gyro nicht verfügbar ist, auf Tastatur umschalten und das in den GameManager übernehmen
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetControlType(GameManager.ControlType.Keyboard);
            }
        }
    }
    
    private System.Collections.IEnumerator CalibrateGyroscopeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Kalibrierung der Gyro-Ausrichtung
        initialGyroAttitude = gyroAttitudeAction.ReadValue<Quaternion>();
        calibrationQuaternion = Quaternion.Inverse(initialGyroAttitude);
        
        Debug.Log("Gyrosensor kalibriert");
    }

    private void Update()
    {
        // Prüfen, ob das Spiel aktiv läuft - keine Bewegung erlauben, wenn es im WaitingToStart oder GameOver Status ist
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            // Keine Bewegung, wenn das Spiel nicht aktiv läuft
            return;
        }

        if (useGyroscope && gyroAvailable)
        {
            // Gyrosensor-Steuerung
            ProcessGyroInput();
        }
        else
        {
            // Keyboard-Steuerung
            ProcessKeyboardInput();
        }
        
        // Kontinuierliche Vorwärtsbewegung in lokaler Z-Richtung
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime, Space.Self);
    }
    
    private void ProcessGyroInput()
    {
        if (!gyroAvailable) return;
        
        // Richtung aus Gyroskop extrahieren
        Vector3 gyroRates = gyroRotationRateAction.ReadValue<Vector3>();
        
        // Horizontale Rotation (Links/Rechts)
        float xRate = invertGyroHorizontal ? -gyroRates.y : gyroRates.y;
        float yRotation = xRate * horizontalRotationSpeed * gyroSensitivity * Time.deltaTime;
        transform.Rotate(0, yRotation, 0);
        
        // Vertikale Rotation (Hoch/Runter) wenn erlaubt
        if (allowVerticalMovement)
        {
            float zRate = invertGyroVertical ? -gyroRates.x : gyroRates.x;
            float xRotation = zRate * verticalRotationSpeed * gyroSensitivity * Time.deltaTime;
            transform.Rotate(xRotation, 0, 0);
        }
    }
    
    private void ProcessKeyboardInput()
    {
        // Vector2-Wert auslesen
        Vector2 input = moveAction.ReadValue<Vector2>();
        
        // Y-Rotation (Links/Rechts drehen)
        float yRotation = input.x * horizontalRotationSpeed * Time.deltaTime;
        transform.Rotate(0, yRotation, 0);
        
        // X-Rotation (Hoch/Runter) wenn erlaubt
        if (allowVerticalMovement)
        {
            float verticalInput = input.y;
            if (invertVerticalControl)
            {
                verticalInput *= -1; // Steuerung umkehren wenn invertVerticalControl aktiviert ist
            }
            
            float xRotation = -verticalInput * verticalRotationSpeed * Time.deltaTime;
            transform.Rotate(xRotation, 0, 0);
        }
    }

    private void OnDestroy()
    {
        moveAction.Disable();
        
        if (gyroAvailable)
        {
            gyroAttitudeAction.Disable();
            gyroRotationRateAction.Disable();
        }
    }
}
