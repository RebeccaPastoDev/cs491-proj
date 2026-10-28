using UnityEngine;
using TMPro;

public class RoverStatusUI : MonoBehaviour
{
    public TMP_Text roverStatus;
    public TMP_Text roverPosition;
    public TMP_Text roverSpeed;
    public TMP_Text roverHeading;
    public Transform rover;
    public Rigidbody roverRigidbody;
    LunarAStarMission mission;

    void Start()
    {
        mission = GetComponent<LunarAStarMission>();
    }
    void Update()
    {
        if (rover != null)
        {
            Vector3 p = rover.position;
            if (roverPosition != null) roverPosition.text = $"POSITION: X: {p.x:F2} | Y: {p.y:F2} | Z: {p.z:F2}";
            if (roverHeading != null) roverHeading.text = $"HEADING: {rover.eulerAngles.y:F1}°";
        }
        if (mission != null)
        {
            if (roverStatus != null) roverStatus.text = "ROVER STATUS: " + mission.State;
            if (roverSpeed != null) roverSpeed.text = $"SPEED: {mission.CurrentSpeed:F2} m/s";
        }
        else if (roverRigidbody != null && roverSpeed != null)
            roverSpeed.text = $"SPEED: {roverRigidbody.linearVelocity.magnitude:F2} m/s";
    }
    public void StartMission() { if (mission != null) mission.StartMission(); }
    public void PauseMission() { if (mission != null) mission.PauseMission(); }
    public void ResetMission() { if (mission != null) mission.ResetMission(); }
}
