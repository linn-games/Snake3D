using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Portal : MonoBehaviour
{
    // Verknüpftes Portal
    public Portal linkedPortal;
    
    // Stärke des Impulses nach der Teleportation
    public float exitForce = 15f;
    
    // Sound für Portal-Effekt
    public AudioSource portalSound;
    
    // Wie weit weg vom Portal der Spieler erscheinen soll
    public float exitDistance = 2.0f;
    
    // Spielfeldmittelpunkt (für präzisere Rotation)
    private readonly Vector3 centerPoint = new Vector3(0f, 20, 0f);
    
    // Portaltransfer im Gange? (verhindert mehrfache Teleportationen)
    private bool isTransferInProgress = false;
    
    // Liste der Objekte, die gerade durch das Portal gehen
    private HashSet<GameObject> objectsInTransit = new HashSet<GameObject>();
    
    private void OnTriggerEnter(Collider other)
    {
        // Überprüfen, ob das Objekt bereits in Übertragung ist (vermeidet Endlosschleifen)
        if (isTransferInProgress || linkedPortal == null || objectsInTransit.Contains(other.gameObject))
            return;
            
        // Position des Zielportals
        Vector3 portalPosition = linkedPortal.transform.position;
        
        // VERBESSERTE RICHTUNGSBERECHNUNG FÜR BESSERE ROTATION:
        // 1. Grundrichtung vom Portal weg
        Vector3 safeDirection = -linkedPortal.transform.forward;
        
        // 2. Leicht in Richtung Zentrum korrigieren für natürlichere Bewegung
        Vector3 directionToCenter = (centerPoint - portalPosition).normalized;
        
        // 3. 
        Vector3 finalDirection = (directionToCenter * 1f).normalized;
        
        // Sichere Position berechnen
        Vector3 targetPosition = portalPosition + finalDirection * exitDistance;
        
        // Neue Rotation - optimierte Richtung
        Quaternion newRotation = Quaternion.LookRotation(finalDirection);
        
        // Verhalten je nach Objekttyp
        if (other.CompareTag("Player"))
        {
            // Spielerkopf erkannt - prüfen, ob es die echte Schlange ist
            SnakeGrowth snakeGrowth = other.GetComponent<SnakeGrowth>();
            if (snakeGrowth != null)
            {
                // Tunnel-Technik für die Schlange
                StartCoroutine(TransportSnakeWithTunnelEffect(snakeGrowth, targetPosition, newRotation));
            }
            else
            {
                // Standard-Teleportation
                TeleportObject(other.gameObject, targetPosition, newRotation, finalDirection);
            }
        }
        else
        {
            // Standard-Teleportation für alle anderen Objekte
            TeleportObject(other.gameObject, targetPosition, newRotation, finalDirection);
        }
        
        // Sound abspielen, wenn vorhanden
        if (portalSound != null)
        {
            portalSound.Play();
        }
    }
    
    // Standard-Teleportationsmethode für einzelne Objekte
    private void TeleportObject(GameObject obj, Vector3 targetPosition, Quaternion newRotation, Vector3 direction)
    {
        objectsInTransit.Add(obj);
        
        // Objekt teleportieren
        obj.transform.position = targetPosition;
        obj.transform.rotation = newRotation;
        
        // Impuls geben, falls es ein Rigidbody hat
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(direction * exitForce, ForceMode.Impulse);
        }
        
        // Nach kurzer Zeit aus der Transit-Liste entfernen
        StartCoroutine(RemoveFromTransitAfterDelay(obj));
    }
    
    // Tunnel-Technik für die Schlange - NUN MIT GetSnakeSegments()
    private IEnumerator TransportSnakeWithTunnelEffect(SnakeGrowth snake, Vector3 targetHeadPosition, Quaternion newRotation)
    {
        // Transferstatus setzen
        isTransferInProgress = true;
        
        // Den Kopf teleportieren
        Transform head = snake.transform;
        objectsInTransit.Add(head.gameObject);
        
        // Kopfposition und Rotation setzen
        head.position = targetHeadPosition;
        head.rotation = newRotation;
        
        // Kurz warten um sicherzustellen, dass der Kopf durch ist
        yield return new WaitForSeconds(0.1f);
        
        // Kopf aus Transit-Liste entfernen, damit er normal bewegt werden kann
        objectsInTransit.Remove(head.gameObject);
        
        // VERBESSERT: GetSnakeSegments() nutzen, wenn verfügbar
        int segmentCount = 0;
        
        try {
            // Direkt die Segmentliste holen
            List<Transform> segments = snake.GetSnakeSegments();
            segmentCount = segments.Count;
            
            // Alle Segmente als "in Transit" markieren, damit sie nicht erneut teleportiert werden
            for (int i = 1; i < segments.Count; i++) // Ab 1, weil 0 der Kopf ist
            {
                if (segments[i] != null)
                {
                    objectsInTransit.Add(segments[i].gameObject);
                }
            }
            
            Debug.Log("GetSnakeSegments() erfolgreich verwendet - " + segmentCount + " Segmente gefunden");
        }
        catch (System.Exception e)
        {
            // Fallback, falls GetSnakeSegments() nicht funktioniert
            Debug.LogWarning("GetSnakeSegments() nicht verfügbar: " + e.Message);
            segmentCount = snake.transform.GetComponentsInChildren<Transform>().Length;
        }
        
        // Mindestens 5 Segmente annehmen (für den Fall, dass die Zählung fehlschlägt)
        segmentCount = Mathf.Max(5, segmentCount);
        
        // Basis-Wartezeit pro Segment berechnen (längere Schlangen brauchen mehr Zeit)
        float waitTimePerSegment = 0.2f;
        
        // Gesamtwartezeit berechnen: Basis + extra Zeit pro Segment
        float totalWaitTime = 0.5f + (waitTimePerSegment * segmentCount);
        
        // Warten, damit alle Segmente nachrücken können
        Debug.Log($"Portal bleibt {totalWaitTime} Sekunden offen für {segmentCount} Segmente");
        yield return new WaitForSeconds(totalWaitTime);
        
        // Alle Segmente aus der Transit-Liste entfernen
        if (snake != null)
        {
            try {
                List<Transform> segments = snake.GetSnakeSegments();
                foreach (Transform segment in segments)
                {
                    if (segment != null)
                    {
                        objectsInTransit.Remove(segment.gameObject);
                    }
                }
            }
            catch {
                // Fallback - keine Aktion nötig, die Liste wird später ohnehin geleert
            }
        }
        
        // Transferstatus zurücksetzen
        isTransferInProgress = false;
    }
    
    // Nach einer kurzen Verzögerung aus der Transit-Liste entfernen
    private IEnumerator RemoveFromTransitAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(0.5f);
        objectsInTransit.Remove(obj);
    }
}