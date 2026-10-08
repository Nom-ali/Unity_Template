
#if UNITY_EDITOR
using UnityEngine;

public class DrawGizmos : MonoBehaviour
{
    // Serialized colors for different Gizmos
    [Header("Gizmo Colors")]
    [SerializeField] private Color color = Color.green;
    [SerializeField] private Color WireColor = Color.green;
    
    void OnDrawGizmos()
    {
        // Handle the BoxCollider
        if (TryGetComponent(out BoxCollider boxCollider))
        {

            // Get the world position of the center of the BoxCollider
            Vector3 worldCenter = transform.position + boxCollider.center;

            // Scale the size based on the GameObject's local scale
            Vector3 scaledSize = new Vector3(
                boxCollider.size.x * transform.localScale.x,
                boxCollider.size.y * transform.localScale.y,
                boxCollider.size.z * transform.localScale.z
            );

            // Draw the wireframe cube to represent the BoxCollider
            Gizmos.color = WireColor;
            Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one); // Apply rotation
            Gizmos.DrawWireCube(Vector3.zero, scaledSize);
            Gizmos.color = color;
            Gizmos.DrawCube(Vector3.zero, scaledSize);
        }
        // Handle the SphereCollider
        else if (TryGetComponent(out SphereCollider sphereCollider))
        {
            // Get the world position of the center of the SphereCollider
            Vector3 worldCenter = transform.position + sphereCollider.center;

            // Scale the radius based on the GameObject's local scale
            float scaledRadius = sphereCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);

            // Draw the wireframe sphere to represent the SphereCollider
            Gizmos.color = WireColor;
            Gizmos.DrawWireSphere(worldCenter, scaledRadius);
            Gizmos.color = color;
            Gizmos.DrawSphere(worldCenter, scaledRadius);
        }
        // Handle the CapsuleCollider
        else if (TryGetComponent(out CapsuleCollider capsuleCollider))
        {
            // Get the world position of the center of the CapsuleCollider
            Vector3 worldCenter = transform.position + capsuleCollider.center;

            // Draw the wireframe capsule to represent the CapsuleCollider
            Gizmos.color = WireColor;
            Gizmos.DrawWireSphere(worldCenter + Vector3.up * capsuleCollider.height / 2f, capsuleCollider.radius); // Top sphere
            Gizmos.DrawWireSphere(worldCenter - Vector3.up * capsuleCollider.height / 2f, capsuleCollider.radius); // Bottom sphere
            Gizmos.DrawLine(worldCenter + Vector3.up * capsuleCollider.height / 2f, worldCenter - Vector3.up * capsuleCollider.height / 2f); // Capsule body
        }
        // Handle other types of colliders (e.g., MeshCollider, etc.)
        else
        {
            Debug.LogWarning("Collider type not handled in the Gizmo drawer.");
        }
    }
}
#endif



