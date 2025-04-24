using UnityEngine;
using System.Collections;

public class SpawnController : MonoBehaviour
{
    [Header("Target-Einstellungen")]
    public GameObject targetPrefab;
    public int maxTargetsActive = 5;
    
    [Header("Spawn-Bereich")]
    public Vector3 spawnAreaSize = new Vector3(20, 10, 20);
    public Vector3 spawnAreaCenter = Vector3.zero;
    public bool visualizeSpawnArea = true;
    
    [Header("Spawn-Timing")]
    public float initialSpawnDelay = 1.0f;
    public float spawnInterval = 3.0f;
    [Range(0f, 5f)]
    public float randomTimeVariation = 1.0f;
    
    [Header("Ziel-Eigenschaften")]
    public bool randomizeRotation = true;
    public bool randomizeScale = false;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    
    [Header("Sound-Einstellungen")]
    [Tooltip("Sound beim Fressen eines Targets")]
    [SerializeField] private AudioClip eatingSound;
    [SerializeField] private AudioSource audioSource;

    // Privates Tracking
    private int currentTargetsCount = 0;
    private Transform playerTransform;
    
    private void Start()
    {
        // Player-Referenz finden
        Player player = FindAnyObjectByType<Player>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Überprüfen, ob ein Target-Prefab zugewiesen ist
        if (targetPrefab == null)
        {
            Debug.LogError("Kein Target-Prefab zugewiesen! Bitte im Inspector hinzufügen.");
            enabled = false;
            return;
        }
        
        // AudioSource überprüfen
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }
        
        // Starten des Spawn-Coroutines
        StartCoroutine(SpawnTargetRoutine());
    }
    
    // Wird aufgerufen, wenn ein Target gegessen wird
    public void OnTargetEaten()
    {
        PlayEatingSound();
    }
    
    // Sound beim Fressen eines Targets abspielen
    public void PlayEatingSound()
    {
        if (audioSource != null && eatingSound != null)
        {
            audioSource.PlayOneShot(eatingSound);
            Debug.Log("Eating sound played");
        }
        else if (eatingSound == null)
        {
            Debug.LogWarning("Kein EatingSound-Clip zugewiesen! Bitte ziehe den 'EatingSnake.wav' in das entsprechende Feld im Inspector.");
        }
    }
    
    private IEnumerator SpawnTargetRoutine()
    {
        // Initiale Wartezeit
        yield return new WaitForSeconds(initialSpawnDelay);
        
        while (true)
        {
            // Nur spawnen, wenn unter dem Maximum
            if (currentTargetsCount < maxTargetsActive)
            {
                SpawnTarget();
            }
            
            // Zufällige Variation zur Wartezeit hinzufügen
            float waitTime = spawnInterval + Random.Range(-randomTimeVariation, randomTimeVariation);
            waitTime = Mathf.Max(0.1f, waitTime); // Mindestens 0.1 Sekunden warten
            
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    private void SpawnTarget()
    {
        // Zufällige Position innerhalb des Spawn-Bereichs
        Vector3 randomPosition = new Vector3(
            Random.Range(-spawnAreaSize.x/2, spawnAreaSize.x/2),
            Random.Range(-spawnAreaSize.y/2, spawnAreaSize.y/2),
            Random.Range(-spawnAreaSize.z/2, spawnAreaSize.z/2)
        ) + spawnAreaCenter;
        
        // Mindestabstand zum Spieler überprüfen, wenn ein Spieler existiert
        if (playerTransform != null)
        {
            float minDistanceToPlayer = 5f;
            if (Vector3.Distance(randomPosition, playerTransform.position) < minDistanceToPlayer)
            {
                // Zu nah am Spieler - position anders wählen
                Vector3 awayFromPlayer = (randomPosition - playerTransform.position).normalized;
                randomPosition = playerTransform.position + awayFromPlayer * minDistanceToPlayer;
            }
        }
        
        // Zufällige Rotation, wenn aktiviert
        Quaternion randomRotation = randomizeRotation ? 
            Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)) : 
            Quaternion.identity;
        
        // Target instantiieren
        GameObject newTarget = Instantiate(targetPrefab, randomPosition, randomRotation);
        
        // Zufällige Größe, wenn aktiviert
        if (randomizeScale)
        {
            float randomScaleFactor = Random.Range(minScale, maxScale);
            newTarget.transform.localScale *= randomScaleFactor;
        }
        
        // Target für Tracking markieren
        currentTargetsCount++;
        
        // TargetController zum Target hinzufügen oder konfigurieren
        TargetController targetController = newTarget.GetComponent<TargetController>();
        if (targetController == null)
        {
            targetController = newTarget.AddComponent<TargetController>();
        }
        
        // TargetController initialisieren
        targetController.Initialize(this);
    }
    
    // Wird vom TargetController aufgerufen, wenn ein Target zerstört wurde
    public void TargetDestroyed()
    {
        currentTargetsCount--;
    }
    
    // Visualisierung des Spawn-Bereichs im Editor
    private void OnDrawGizmos()
    {
        if (visualizeSpawnArea)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.2f);
            Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);
            
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
        }
    }
}
