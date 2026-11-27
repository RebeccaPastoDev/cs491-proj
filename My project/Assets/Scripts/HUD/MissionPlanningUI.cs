using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionPlanningUI : MonoBehaviour
{
    [Header("Task Toggles")]
    public Toggle findWaterToggle;
    public Toggle collectWaterToggle;
    public Toggle depositWaterToggle;
    public Toggle returnToBaseToggle;

    [Header("Planning UI")]
    public TMP_Text assignedTasksText;
    public Button assignMissionButton;

    [Header("Progress UI")]
    public TMP_Text routeOverviewText;
    public TMP_Text progressText;

    [Header("Navigation")]
    public SwitchHUDPage pageSwitcher;

    [Header("VR Task Toggles")]
    public Toggle vrFindWaterToggle;
    public Toggle vrCollectWaterToggle;
    public Toggle vrDepositWaterToggle;
    public Toggle vrReturnToBaseToggle;

    [Header("VR Planning UI")]
    public TMP_Text vrAssignedTasksText;
    public Button vrAssignMissionButton;

    [Header("Unity Bridge")]
    public UnityBridgeClient bridgeClient;

    private bool awaitingAcknowledgment;
    private float acknowledgmentDeadline;
    private string pendingMissionId;
    private string validatedMissionId;
    private int submittedGeneration;
    private bool submitted;
    private string localStatus;

    private readonly List<string> selectedTasks =
        new List<string>();

    [Serializable]
    public class MissionWaypoint
    {
        public float x;
        public float z;

        public MissionWaypoint(float x, float z)
        {
            this.x = x;
            this.z = z;
        }
    }

    [Serializable]
    public class MissionRequest
    {
        public string type = "mission_start";
        public string schema_version = "1.0";
        public string mission_id;
        public string mission_name = "fetch_water";
        public string coordinate_frame = "unity_world";
        public MissionWaypoint[] waypoints;
    }

    [Serializable]
    public class MissionAcknowledgment
    {
        public bool accepted;
        public bool physical_execution_enabled;
        public string error;
        public MissionDetails mission;
    }

    [Serializable]
    public class MissionDetails
    {
        public string mission_id;
        public string status;
        public bool execution_enabled;
    }

    void Start()
    {
        BindTogglePair(findWaterToggle, vrFindWaterToggle);
        BindTogglePair(collectWaterToggle, vrCollectWaterToggle);
        BindTogglePair(depositWaterToggle, vrDepositWaterToggle);
        BindTogglePair(returnToBaseToggle, vrReturnToBaseToggle);

        assignMissionButton.onClick.AddListener(AssignMission);
        if (vrAssignMissionButton != null)
            vrAssignMissionButton.onClick.AddListener(AssignMission);

        if (bridgeClient != null)
        {
            bridgeClient.OnMissionAcknowledgment +=
                HandleMissionAcknowledgment;
        }

        UpdateAssignedTasks();
    }

    private void BindTogglePair(Toggle desktop, Toggle vr)
    {
        if (vr != null)
            vr.SetIsOnWithoutNotify(desktop.isOn);

        desktop.onValueChanged.AddListener(value =>
        {
            if (vr != null)
                vr.SetIsOnWithoutNotify(value);
            UpdateAssignedTasks();
        });

        if (vr != null)
        {
            vr.onValueChanged.AddListener(value =>
            {
                desktop.SetIsOnWithoutNotify(value);
                UpdateAssignedTasks();
            });
        }
    }

    void UpdateAssignedTasks()
    {
        selectedTasks.Clear();

        if (findWaterToggle.isOn)
            selectedTasks.Add("Find Water");

        if (collectWaterToggle.isOn)
            selectedTasks.Add("Collect Water");

        if (depositWaterToggle.isOn)
            selectedTasks.Add("Deposit Water at Tank");

        if (returnToBaseToggle.isOn)
            selectedTasks.Add("Return to Base");

        string taskList = "";

        if (selectedTasks.Count == 0)
        {
            taskList = "No tasks selected";
        }
        else
        {
            for (int i = 0; i < selectedTasks.Count; i++)
            {
                taskList +=
                    $"{i + 1:00} {selectedTasks[i]}\n";
            }
        }

        // Only update the displayed text when it changes.
        if (assignedTasksText != null &&
            assignedTasksText.text != taskList)
        {
            assignedTasksText.text = taskList;
        }

        if (vrAssignedTasksText != null &&
            vrAssignedTasksText.text != taskList)
            vrAssignedTasksText.text = taskList;

        if (vrAssignMissionButton != null)
            vrAssignMissionButton.interactable =
                selectedTasks.Count > 0 && !awaitingAcknowledgment;

        // Disable assignment when no tasks are selected.
        if (assignMissionButton != null)
        {
            assignMissionButton.interactable =
                selectedTasks.Count > 0 &&
                !awaitingAcknowledgment;
        }
    }

    public async void AssignMission()
    {
        // Refresh the selected tasks before submission.
        UpdateAssignedTasks();

        if (selectedTasks.Count == 0 ||
            awaitingAcknowledgment)
            return;

        List<string> destinations = new List<string>();

        destinations.Add("Base Camp");

        if (findWaterToggle.isOn)
            destinations.Add("Water Search Area");

        if (collectWaterToggle.isOn)
            destinations.Add("Water Collection Site");

        if (depositWaterToggle.isOn)
            destinations.Add("Water Tank");

        if (returnToBaseToggle.isOn)
            destinations.Add("Base Camp");

        if (routeOverviewText != null)
        {
            routeOverviewText.text =
                string.Join("\n    ↓\n", destinations);
        }


        if (pageSwitcher != null)
            pageSwitcher.ShowProgress();

        if (bridgeClient == null ||
            !bridgeClient.TelemetryActive)
        {
            SetStatus("Bridge unavailable");
            return;
        }

        // Only allow recorded-data validation.
        if (bridgeClient.isLive ||
            bridgeClient.physicalExecutionEnabled ||
            bridgeClient.telemetrySource != "recorded")
        {
            SetStatus("Test mode required");
            return;
        }

        pendingMissionId =
            "fetch_water_" + Guid.NewGuid().ToString("N");

        submittedGeneration =
            bridgeClient.connectionGeneration;

        submitted = true;
        validatedMissionId = null;
        localStatus = null;

        MissionRequest mission = new MissionRequest
        {
            mission_id = pendingMissionId,

            // Valid temporary sample waypoints.
            waypoints = new MissionWaypoint[]
            {
                new MissionWaypoint(0f, 0f),
                new MissionWaypoint(0f, 10f),
                new MissionWaypoint(10f, 10f),
                new MissionWaypoint(10f, 30f)
            }
        };

        awaitingAcknowledgment = true;

        acknowledgmentDeadline =
            Time.realtimeSinceStartup + 10f;

        UpdateAssignedTasks();

        SetStatus("Awaiting acknowledgment");

        string json = JsonUtility.ToJson(mission);

        Debug.Log("[Mission Planning] Sending: " + json);

        await bridgeClient.SendMessageToBridge(json);
    }

    private void SetStatus(string status)
    {

        if (progressText != null &&
            progressText.text != status)
        {
            progressText.text = status;
        }
    }

    void Update()
    {
        // Keep task selection independent of bridge status.
        UpdateAssignedTasks();

        // Handle connection loss.
        if (bridgeClient == null ||
            !bridgeClient.TelemetryActive)
        {
            if (awaitingAcknowledgment)
            {
                awaitingAcknowledgment = false;

                localStatus =
                    "Submission interrupted - reassign mission";

                UpdateAssignedTasks();
            }

            SetStatus("Bridge unavailable");
            return;
        }

        // Detect bridge reconnection.
        if (submitted &&
            submittedGeneration !=
            bridgeClient.connectionGeneration)
        {
            submitted = false;
            awaitingAcknowledgment = false;
            validatedMissionId = null;

            localStatus =
                "Mission state reset - reassign mission";

            UpdateAssignedTasks();
        }

        // Handle acknowledgment timeout.
        if (awaitingAcknowledgment)
        {
            if (Time.realtimeSinceStartup <
                acknowledgmentDeadline)
            {
                SetStatus("Awaiting acknowledgment");
                return;
            }

            awaitingAcknowledgment = false;
            localStatus = "Acknowledgment timeout";

            UpdateAssignedTasks();
        }

        // Synchronize mission status.
        if (validatedMissionId != null &&
            bridgeClient.missionId == validatedMissionId &&
            bridgeClient.missionStatus == "validated" &&
            bridgeClient.missionSource == "mock")
        {
            SetStatus(
                bridgeClient.physicalExecutionEnabled ||
                bridgeClient.isLive
                    ? "Test mode required"
                    : "Validated - Execution disabled");
        }
        else if (localStatus != null)
        {
            SetStatus(localStatus);
        }
        else if (validatedMissionId != null)
        {
            SetStatus("Awaiting mission telemetry");
        }
        else if (bridgeClient.missionStatus == "idle")
        {
            SetStatus("No mission assigned");
        }
        else
        {
            SetStatus("Mission status unavailable");
        }
    }

    private void HandleMissionAcknowledgment(string json)
    {
        if (!awaitingAcknowledgment ||
            bridgeClient.connectionGeneration !=
            submittedGeneration)
        {
            return;
        }

        MissionAcknowledgment ack;

        try
        {
            ack =
                JsonUtility.FromJson<MissionAcknowledgment>(
                    json);
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "[Mission Planning] Invalid acknowledgment: " +
                e.Message);

            return;
        }

        if (ack == null)
            return;

        if (ack.accepted &&
            (ack.mission == null ||
             ack.mission.mission_id != pendingMissionId))
        {
            return;
        }

        awaitingAcknowledgment = false;

        // Handle rejected missions.
        if (!ack.accepted)
        {
            localStatus = "Mission rejected";

            Debug.LogWarning(
                "[Mission Planning] Rejected: " + ack.error);
        }
        else if (ack.physical_execution_enabled ||
                 ack.mission.execution_enabled)
        {
            localStatus = "Unexpected execution mode";
        }
        else
        {
            validatedMissionId = ack.mission.mission_id;
            localStatus = null;
        }

        Debug.Log("[Mission Planning] Response: " + json);

        SetStatus(
            localStatus ?? "Awaiting mission telemetry");

        UpdateAssignedTasks();
    }

    private void OnDestroy()
    {
        if (assignMissionButton != null)
            assignMissionButton.onClick.RemoveListener(AssignMission);
        if (vrAssignMissionButton != null)
            vrAssignMissionButton.onClick.RemoveListener(AssignMission);

        if (bridgeClient != null)
        {
            bridgeClient.OnMissionAcknowledgment -=
                HandleMissionAcknowledgment;
        }
    }
}
