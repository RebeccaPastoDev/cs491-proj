using UnityEngine;
using TMPro;

public class RoverTerrainMonitor : MonoBehaviour
{
    [Header("Rover")]
    public Transform rover;

    [Header("Ground Detection")]
    public float rayStartHeight = 50f;
    public float rayDistance = 500f;
    public LayerMask groundLayers = ~0;

    [Header("Desktop HUD")]
    public TMP_Text slopeValue;

    [Header("VR HUD")]
    public TMP_Text vrSlopeValue;

    [Header("Slope Thresholds")]
    public float cautionSlope = 10f;
    public float warningSlope = 15f;

    // Values accessible by RoverWarningManager.
    public bool HasGround { get; private set; }
    public float CurrentSlope { get; private set; }

    void Update()
    {
        if (rover == null)
        {
            HasGround = false;
            UpdateSlopeDisplay(
                "No rover assigned",
                Color.gray
            );
            return;
        }

        // Start the ray above the rover.
        Vector3 origin =
            rover.position + Vector3.up * rayStartHeight;

        // Cast downward to detect lunar terrain.
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            rayDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        float nearestDistance = Mathf.Infinity;
        RaycastHit groundHit = default;
        bool foundGround = false;

        foreach (RaycastHit hit in hits)
        {
            // Ignore the rover's own colliders.
            if (hit.collider.transform == rover ||
                hit.collider.transform.IsChildOf(rover))
            {
                continue;
            }

            // Select the nearest valid surface.
            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                groundHit = hit;
                foundGround = true;
            }
        }

        // Handle areas without terrain collision.
        if (!foundGround)
        {
            HasGround = false;

            UpdateSlopeDisplay(
                "No ground detected",
                Color.gray
            );

            return;
        }

        // Calculate the slope angle.
        float slope = Vector3.Angle(
            groundHit.normal,
            Vector3.up
        );

        // Share measurements with the warning manager.
        HasGround = true;
        CurrentSlope = slope;

        Color slopeColor;

        if (slope >= warningSlope)
        {
            // Red: hazardous slope.
            slopeColor = new Color32(
                255, 100, 100, 255
            );
        }
        else if (slope >= cautionSlope)
        {
            // Yellow: caution.
            slopeColor = new Color32(
                255, 200, 80, 255
            );
        }
        else
        {
            // Green: normal terrain.
            slopeColor = new Color32(
                119, 201, 154, 255
            );
        }

        // Update desktop and VR displays.
        UpdateSlopeDisplay(
            $"{slope:F1}°",
            slopeColor
        );
    }

    void UpdateSlopeDisplay(string value, Color color)
    {
        // Desktop HUD.
        if (slopeValue != null)
        {
            slopeValue.text = value;
            slopeValue.color = color;
        }

        // VR HUD.
        if (vrSlopeValue != null)
        {
            vrSlopeValue.text = value;
            vrSlopeValue.color = color;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (rover == null)
            return;

        Gizmos.color = Color.yellow;

        Vector3 origin =
            rover.position + Vector3.up * rayStartHeight;

        Gizmos.DrawLine(
            origin,
            origin + Vector3.down * rayDistance
        );
    }
}
