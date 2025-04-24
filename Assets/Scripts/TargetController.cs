using UnityEngine;

public class TargetController : MonoBehaviour
{
    private SpawnController spawnController;
    
    [Header("Effekte")]
    public bool rotate = true;
    public float rotationSpeed = 50f;
    public bool hover = true;
    public float hoverHeight = 0.5f;
    public float hoverSpeed = 1f;
    
    private Vector3 startPosition;
    private float hoverOffset = 0f;
    
    public void Initialize(SpawnController controller)
    {
        spawnController = controller;
        startPosition = transform.position;
        
        // Zufälligen Hover-Offset setzen, damit nicht alle Targets synchron schweben
        hoverOffset = Random.Range(0f, Mathf.PI * 2);
        
        // Sicherstellen, dass das GameObject den Tag "Target" hat
        if (gameObject.tag != "Target")
        {
            gameObject.tag = "Target";
        }
    }
    
    private void Update()
    {
        // Drehung
        if (rotate)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
        
        // Schwebende Bewegung
        if (hover)
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + hoverOffset) * hoverSpeed) * hoverHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    // Wenn das Target zerstört wird, den SpawnController benachrichtigen
    private void OnDestroy()
    {
        if (spawnController != null)
        {
            spawnController.TargetDestroyed();
        }
    }
}
