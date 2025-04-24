using System.Collections.Generic;
using UnityEngine;

public class SnakeGrowth : MonoBehaviour
{
    [Header("Body Segment Prefab")]
    [Tooltip("Ziehe dein BODY PREFAB hier hinein!")]
    [SerializeField]
    public GameObject segmentPrefab; 
    
    [Header("Segment-Einstellungen")]
    [Tooltip("Abstand zwischen den Segmenten")]
    public float segmentSpacing = 1.0f;
    
    [Tooltip("Anzahl der Segmente zu Spielbeginn")]
    public int initialSegments = 0;
    
    // Liste der Segmente
    private List<Transform> segments = new List<Transform>();
    
    private void Start()
    {
        // Prüfen, ob das segmentPrefab gesetzt ist
        if (segmentPrefab == null)
        {
            Debug.LogError("FEHLER: Bitte ziehe dein BODY PREFAB in das 'segmentPrefab'-Feld im Inspector!");
            return;
        }
        
        // Kopf zur Liste hinzufügen
        segments.Add(transform);
        
        // Initiale Segmente erstellen
        for (int i = 0; i < initialSegments; i++)
        {
            Grow();
        }
        
        Debug.Log("SnakeGrowth initialisiert. Body Prefab: " + (segmentPrefab != null ? segmentPrefab.name : "NICHT GESETZT!"));
    }
    
    private void LateUpdate()
    {
        // Segmente bewegen - jetzt viel einfacher mit direktem Snap
        MoveSegmentsWithSnap();
    }
    
    // Bewegt alle Segmente mit einfachem Snap zum vorherigen Segment
    private void MoveSegmentsWithSnap()
    {
        // Wir starten bei Index 1 (das erste Segment nach dem Kopf)
        // und arbeiten rückwärts, damit kein "Ziehharmonika-Effekt" entsteht
        for (int i = segments.Count - 1; i > 0; i--)
        {
            if (segments[i] == null || segments[i-1] == null) continue;
            
            // Berechne die Position direkt hinter dem vorherigen Segment
            Vector3 directionFromPrevious = -segments[i-1].forward.normalized;
            Vector3 targetPosition = segments[i-1].position + directionFromPrevious * segmentSpacing;
            
            // Setze Position und Rotation direkt - kein Lerp/Slerp mehr
            segments[i].position = targetPosition;
            segments[i].rotation = segments[i-1].rotation;
        }
    }
    
    // Wachsen - wird aufgerufen, wenn ein Target eingesammelt wird
    public void Grow()
    {
        if (segmentPrefab == null)
        {
            Debug.LogError("SnakeGrowth: segmentPrefab ist nicht gesetzt!");
            return;
        }
        
        // Position für das neue Segment berechnen
        Transform lastSegment = segments[segments.Count - 1];
        Vector3 spawnDirection = -lastSegment.forward;
        Vector3 spawnPosition = lastSegment.position + spawnDirection * segmentSpacing;
        
        // Neues Segment erstellen
        GameObject newSegment = Instantiate(segmentPrefab, spawnPosition, lastSegment.rotation);
        
        // Tag setzen
        newSegment.tag = "Body";
        
        // Zur Liste hinzufügen
        segments.Add(newSegment.transform);
    }
    
    // Methode zum Teleportieren aller Segmente
    public void TeleportAllSegments(Vector3 newHeadPosition, Quaternion newHeadRotation)
    {
        // Kopf teleportieren
        transform.position = newHeadPosition;
        transform.rotation = newHeadRotation;
        
        // Alle anderen Segmente in einer Linie hinter dem Kopf platzieren
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                // Berechne Position direkt hinter dem vorherigen Segment
                Vector3 position = segments[i-1].position - (segments[i-1].forward * segmentSpacing);
                segments[i].position = position;
                segments[i].rotation = newHeadRotation;
            }
        }
    }
    
    // UMBENANNT: Segmente unverwundbar machen
    public void MakeSegmentsInvulnerable()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                segments[i].gameObject.tag = "InvulnerableSegment";
            }
        }
    }
    
    // UMBENANNT: Segmente verwundbar machen
    public void MakeSegmentsVulnerable()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                segments[i].gameObject.tag = "Body";
            }
        }
    }
    
    // Zurücksetzen
    public void ResetSegments()
    {
        // Alle Segmente außer dem Kopf entfernen
        for (int i = segments.Count - 1; i > 0; i--)
        {
            if (segments[i] != null)
            {
                Destroy(segments[i].gameObject);
            }
        }
        
        // Liste zurücksetzen, aber Kopf behalten
        Transform head = segments[0];
        segments.Clear();
        segments.Add(head);
        
        // Initiale Segmente neu erstellen
        for (int i = 0; i < initialSegments; i++)
        {
            Grow();
        }
    }
    
    // Methode, um den Index eines Segments in der Liste zu ermitteln
    public int GetSegmentIndex(Transform segmentTransform)
    {
        // In der Liste nach dem Segment suchen
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == segmentTransform)
            {
                return i;
            }
        }
        
        // Segment nicht gefunden
        return -1;
    }
    
    // NEUE METHODE: Gibt alle Segmente (inkl. Kopf) zurück
    public List<Transform> GetSnakeSegments()
    {
        return segments;
    }
}
