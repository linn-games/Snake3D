using UnityEngine;

public class Walls : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Nur auf Player und Body reagieren
        if (!other.CompareTag("Player") && !other.CompareTag("Body"))
            return;
            
        // VEREINFACHT: Keine Kollision, wenn unverwundbar
        if (other.CompareTag("InvulnerableSegment"))
            return;
            
        // Spieler töten
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.Die();
        }
        else
        {
            // Prüfen, ob es ein Körperteil ist, und den Spieler finden
            player = FindAnyObjectByType<Player>();
            if (player != null)
            {
                player.Die();
            }
        }
    }
}
