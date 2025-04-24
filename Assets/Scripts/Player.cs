using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("Game Settings")]
    public Vector3 startPosition = Vector3.zero;
    
    public Rigidbody rb;

    private SnakeGrowth snakeGrowth;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationZ;
        
        // SnakeGrowth-Komponente speichern
        snakeGrowth = GetComponent<SnakeGrowth>();
        
        // Anfangsposition für Reset speichern
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        // Spieler-Tag setzen
        gameObject.tag = "Player";
    }

    private void OnTriggerEnter(Collider other)
    {
        // Nicht reagieren, wenn das Spiel nicht läuft
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;
        
        // EINFACHE TAG-PRÜFUNG: Wenn wir unverwundbar sind, wird keine Kollision ausgelöst
        if (gameObject.CompareTag("InvulnerableSegment"))
            return;
        
        // Verschiedene Kollisionstypen behandeln
        if (other.CompareTag("Target"))
        {
            // Target einsammeln
            HandleTargetCollision(other.gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            // Mit Wand kollidiert - stirbt nur wenn nicht unverwundbar
            Die();
        }
        else if (other.CompareTag("Body"))
        {
            // Mit Körpersegment kollidiert - prüfen, ob es ein direkt benachbartes Segment ist
            HandleBodyCollision(other.transform);
        }
    }

    // Target-Kollision verarbeiten
    private void HandleTargetCollision(GameObject target)
    {
        // Sound abspielen
        SpawnController spawnController = FindAnyObjectByType<SpawnController>();
        if (spawnController != null)
        {
            spawnController.PlayEatingSound();
        }
        
        // Target zerstören und wachsen
        Destroy(target);
        if (snakeGrowth != null)
        {
            snakeGrowth.Grow();
        }
        
        // Punkte erhöhen
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }
    }

    // Körpersegment-Kollision verarbeiten
    private void HandleBodyCollision(Transform segmentTransform)
    {
        // Segment-Index ermitteln
        int segmentIndex = GetSegmentIndex(segmentTransform);
        
        // Regel: Jedes Segment darf nur mit dem Segment direkt vor und hinter ihm kollidieren
        // Kopf ist Index 0, erstes Segment ist Index 1, usw.
        if (segmentIndex > 1)
        {
            // Tod nur auslösen, wenn mit einem Segment kollidiert wird, das weder Vorgänger noch Nachfolger ist
            // Segment 0 (Kopf) darf Segment 1 berühren, aber nicht Segment 2+
            Die();
        }
    }

    // Hilfsmethode, um den Index eines Segments in der Schlange zu ermitteln
    private int GetSegmentIndex(Transform segmentTransform)
    {
        if (snakeGrowth != null)
        {
            return snakeGrowth.GetSegmentIndex(segmentTransform);
        }
        return -1;
    }
    
    // Spieler stirbt
    public void Die()
    {
        // Nicht nochmal sterben, wenn das Spiel schon vorbei ist
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;
        
        // Nicht sterben, wenn unverwundbar
        if (gameObject.CompareTag("InvulnerableSegment"))
            return;
        
        // GameManager benachrichtigen
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        
        // Nach kurzer Verzögerung zurücksetzen
        Invoke("ResetPlayer", 1.5f);
    }
    
    // Spieler zurücksetzen
    private void ResetPlayer()
    {
        // Position und Rotation zurücksetzen
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        // Geschwindigkeit zurücksetzen
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Schlange zurücksetzen
        if (snakeGrowth != null)
        {
            snakeGrowth.ResetSegments();
        }
        
        // GameManager über Reset informieren
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
    }
}
