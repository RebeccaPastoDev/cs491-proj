using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoverStatusUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text roverStatus;
    public TMP_Text roverPosition;
    public TMP_Text roverSpeed;
    public TMP_Text roverHeading;
    public Image statusDot;
    public TMP_Text missionProgress;

    [Header("Rover References")]
    public Transform rover;
    public Rigidbody roverRigidbody;
    public MecanumRoverController roverController;

    // Save rover's starting position and rotation
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private bool startingPositionSaved = false;

    void Start()
    {
        // Remember initial rover position and rotation
        if (roverRigidbody != null)
        {
            startingPosition = roverRigidbody.position;
            startingRotation = roverRigidbody.rotation;
            startingPositionSaved = true;
        }
        else if (rover != null)
        {
            startingPosition = rover.position;
            startingRotation = rover.rotation;
            startingPositionSaved = true;
        }

        // Initialize HUD
        SetStatus(false);

        if (missionProgress != null)
            missionProgress.text = "Not started";
    }

    void Update()
    {
        // Update rover position and heading
        if (rover != null)
        {
            Vector3 position = rover.position;

            if (roverPosition != null)
            {
                roverPosition.text =
                    $"X: {position.x:F2}  Y: {position.y:F2}  Z: {position.z:F2}";
            }

            if (roverHeading != null)
            {
                roverHeading.text = $"{rover.eulerAngles.y:F1}°";
            }
        }

        // Update rover speed
        if (roverRigidbody != null && roverSpeed != null)
        {
            float speed = roverRigidbody.linearVelocity.magnitude;
            roverSpeed.text = $"{speed:F2} m/s";
        }
    }

    void SetStatus(bool online)
    {
        Color statusColor = online
            ? new Color32(119, 201, 154, 255)
            : new Color32(242, 200, 121, 255);

        if (roverStatus != null)
        {
            roverStatus.text = online ? "Online" : "Standby";
            roverStatus.color = statusColor;
        }

        if (statusDot != null)
            statusDot.color = statusColor;
    }

    public void StartMission()
    {
        SetStatus(true);

        if (missionProgress != null)
            missionProgress.text = "In progress";
    }

    public void PauseMission()
    {
        SetStatus(false);

        if (missionProgress != null)
            missionProgress.text = "Paused";
    }

    public void ResetMission()
    {
        // Stop rover movement
        if (roverRigidbody != null)
        {
            roverRigidbody.linearVelocity = Vector3.zero;
            roverRigidbody.angularVelocity = Vector3.zero;
        }

        // Restore starting position and rotation
        if (startingPositionSaved)
        {
            if (roverRigidbody != null)
            {
                roverRigidbody.position = startingPosition;
                roverRigidbody.rotation = startingRotation;
            }
            else if (rover != null)
            {
                rover.SetPositionAndRotation(
                    startingPosition,
                    startingRotation
                );
            }
        }

        // Reset HUD
        SetStatus(false);

        if (missionProgress != null)
            missionProgress.text = "Not started";
    }
}
