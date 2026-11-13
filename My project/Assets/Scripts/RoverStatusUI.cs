
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoverStatusUI : MonoBehaviour
{
    [Header("Desktop UI References")]
    public TMP_Text roverStatus;
    public TMP_Text roverPosition;
    public TMP_Text roverSpeed;
    public TMP_Text roverHeading;
    public Image statusDot;
    public TMP_Text missionProgress;

    [Header("VR UI References")]
    public TMP_Text vrRoverStatus;
    public TMP_Text vrRoverPosition;
    public TMP_Text vrRoverSpeed;
    public TMP_Text vrRoverHeading;
    public Image vrStatusDot;
    public TMP_Text vrMissionProgress;

    [Header("Rover References")]
    public Transform rover;
    public Rigidbody roverRigidbody;
    public MecanumRoverController roverController;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private bool startingPositionSaved;

    void Start()
    {
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

        SetStatus(false);
        SetProgress("Not started");
    }

    void Update()
    {
        if (rover != null)
        {
            Vector3 position = rover.position;
            string positionText =
                $"X: {position.x:F2}  Y: {position.y:F2}  Z: {position.z:F2}";
            string headingText = $"{rover.eulerAngles.y:F1}°";

            SetText(roverPosition, vrRoverPosition, positionText);
            SetText(roverHeading, vrRoverHeading, headingText);
        }

        if (roverRigidbody != null)
        {
            string speedText =
                $"{roverRigidbody.linearVelocity.magnitude:F2} m/s";

            SetText(roverSpeed, vrRoverSpeed, speedText);
        }
    }

    void SetText(TMP_Text desktop, TMP_Text vr, string value)
    {
        if (desktop != null)
            desktop.text = value;

        if (vr != null)
            vr.text = value;
    }

    void SetStatus(bool online)
    {
        Color color = online
            ? new Color32(119, 201, 154, 255)
            : new Color32(242, 200, 121, 255);

        string status = online ? "Online" : "Standby";

        SetText(roverStatus, vrRoverStatus, status);

        if (roverStatus != null)
            roverStatus.color = color;

        if (vrRoverStatus != null)
            vrRoverStatus.color = color;

        if (statusDot != null)
            statusDot.color = color;

        if (vrStatusDot != null)
            vrStatusDot.color = color;
    }

    public void SetProgress(string value)
    {
        SetText(missionProgress, vrMissionProgress, value);
    }

    public void StartMission()
    {
        SetStatus(true);
        SetProgress("In progress");
    }

    public void PauseMission()
    {
        SetStatus(false);
        SetProgress("Paused");
    }

    public void ResetMission()
    {
        if (roverRigidbody != null)
        {
            roverRigidbody.linearVelocity = Vector3.zero;
            roverRigidbody.angularVelocity = Vector3.zero;
        }

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

        SetStatus(false);
        SetProgress("Not started");
    }
}
