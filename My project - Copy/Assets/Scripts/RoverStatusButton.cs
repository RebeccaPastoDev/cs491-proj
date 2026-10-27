
using UnityEngine;
using TMPro;

public class RoverStatusUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text roverStatus;
    public TMP_Text roverPosition;
    public TMP_Text roverSpeed;
    public TMP_Text roverHeading;

    [Header("Rover References")]
    public Transform rover;
    public Rigidbody roverRigidbody;

    void Update()
    {
        //live rover coordinates
        if (rover != null && roverPosition != null)
        {
            Vector3 position = rover.position;

            roverPosition.text =
                $"POSITION: X: {position.x:F2} | Y: {position.y:F2} | Z: {position.z:F2}";

            // live rover heading in degrees
            float heading = rover.eulerAngles.y;

            if (roverHeading != null)
            {
                roverHeading.text = $"HEADING: {heading:F1}°";
            }
        }

        // live rover speed
        if (roverRigidbody != null && roverSpeed != null)
        {
            float speed = roverRigidbody.linearVelocity.magnitude;

            roverSpeed.text = $"SPEED: {speed:F2} m/s";
        }
    }

    public void StartMission()
    {
        roverStatus.text = "ROVER STATUS: ONLINE";
    }

    public void PauseMission()
    {
        roverStatus.text = "ROVER STATUS: STANDBY";
    }

    public void ResetMission()
    {
        roverStatus.text = "ROVER STATUS: STANDBY";
    }
}
