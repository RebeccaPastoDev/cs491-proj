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

    private readonly List<string> selectedTasks = new List<string>();

    void Start()
    {
        // Listen for task selection changes
        findWaterToggle.onValueChanged.AddListener(
            value => UpdateAssignedTasks());

        collectWaterToggle.onValueChanged.AddListener(
            value => UpdateAssignedTasks());

        depositWaterToggle.onValueChanged.AddListener(
            value => UpdateAssignedTasks());

        returnToBaseToggle.onValueChanged.AddListener(
            value => UpdateAssignedTasks());

        // Assign mission button
        assignMissionButton.onClick.AddListener(AssignMission);

        // Initialize assigned tasks
        UpdateAssignedTasks();
    }

    void UpdateAssignedTasks()
    {
        selectedTasks.Clear();

        // Add selected tasks in mission order
        if (findWaterToggle.isOn)
            selectedTasks.Add("Find Water");

        if (collectWaterToggle.isOn)
            selectedTasks.Add("Collect Water");

        if (depositWaterToggle.isOn)
            selectedTasks.Add("Deposit Water at Tank");

        if (returnToBaseToggle.isOn)
            selectedTasks.Add("Return to Base");

        // Update Planning page
        if (selectedTasks.Count == 0)
        {
            assignedTasksText.text = "No tasks selected";
        }
        else
        {
            assignedTasksText.text = "";

            for (int i = 0; i < selectedTasks.Count; i++)
            {
                assignedTasksText.text +=
                    $"{i + 1:00} {selectedTasks[i]}\n";
            }
        }

        // Disable assignment when no tasks are selected
        assignMissionButton.interactable =
            selectedTasks.Count > 0;
    }

    public void AssignMission()
    {
        if (selectedTasks.Count == 0)
            return;

        // Build route overview
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

        // Update Progress page route
        if (routeOverviewText != null)
        {
            routeOverviewText.text =
                string.Join("\n    ↓\n", destinations);
        }

        // Reset mission progress display
        if (progressText != null)
        {
            progressText.text = "Assigned - Not started";
        }

        // Automatically open Mission Progress page
        if (pageSwitcher != null)
        {
            pageSwitcher.ShowProgress();
        }
    }
}
