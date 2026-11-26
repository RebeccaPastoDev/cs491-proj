using UnityEngine;

public class SwitchHUDPage : MonoBehaviour
{
    public GameObject monitoringBar;
    public GameObject missionPlanningBar;
    public GameObject missionProgressBar;

    void Start()
    {
        ShowMonitoring();
    }

    public void ShowMonitoring()
    {
        monitoringBar.SetActive(true);
        missionPlanningBar.SetActive(false);
        missionProgressBar.SetActive(false);
    }

    public void ShowPlanning()
    {
        monitoringBar.SetActive(false);
        missionPlanningBar.SetActive(true);
        missionProgressBar.SetActive(false);
    }

    public void ShowProgress()
    {
        monitoringBar.SetActive(false);
        missionPlanningBar.SetActive(false);
        missionProgressBar.SetActive(true);
    }
}
