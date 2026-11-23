using UnityEngine;
using System.Collections;
using CesiumForUnity;

[RequireComponent(typeof(Rigidbody))]
public class RoverSpawnSafety : MonoBehaviour
{
    [Header("Terrain Detection")]
    public Cesium3DTileset lunarTerrain;
    public float rayStartHeight = 10f;
    public float rayDistance = 100f;
    public float groundClearance = 0.5f;
    public float checkInterval = 0.2f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Rigidbody rb;
    private bool originalKinematic;
    private bool originalGravity;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        spawnPosition = rb.position;
        spawnRotation = rb.rotation;

        originalKinematic = rb.isKinematic;
        originalGravity = rb.useGravity;

        // Hold the rover before physics starts.
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    IEnumerator Start()
    {
        if (lunarTerrain == null)
        {
            Debug.LogError(
                "[RoverSpawnSafety] Lunar terrain is not assigned."
            );
            yield break;
        }

        if (showDebugLogs)
            Debug.Log("[RoverSpawnSafety] Waiting for lunar terrain collider...");

        while (true)
        {
            Vector3 rayOrigin =
                spawnPosition + Vector3.up * rayStartHeight;

            RaycastHit[] hits = Physics.RaycastAll(
                rayOrigin,
                Vector3.down,
                rayDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            bool terrainFound = false;
            float groundHeight = float.NegativeInfinity;

            foreach (RaycastHit hit in hits)
            {
                // Only accept colliders belonging to the
                // assigned Cesium terrain tileset.
                Cesium3DTileset hitTileset =
                    hit.collider.GetComponentInParent<Cesium3DTileset>();

                if (hitTileset != lunarTerrain)
                    continue;

                if (hit.point.y > groundHeight)
                {
                    groundHeight = hit.point.y;
                    terrainFound = true;
                }
            }

            if (terrainFound)
            {
                // Keep the rover at its original X/Z position.
                // Raise it if necessary to avoid spawning
                // inside the terrain.
                Vector3 safePosition = spawnPosition;

                float minimumY =
                    groundHeight + groundClearance;

                if (safePosition.y < minimumY)
                    safePosition.y = minimumY;

                rb.position = safePosition;
                rb.rotation = spawnRotation;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                Physics.SyncTransforms();

                // Restore the original physics settings.
                rb.isKinematic = originalKinematic;
                rb.useGravity = originalGravity;

                if (showDebugLogs)
                {
                    Debug.Log(
                        "[RoverSpawnSafety] Terrain detected. " +
                        "Releasing rover."
                    );
                }

                yield break;
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }
}
