
using UnityEngine;

public class SwitchHUDPage : MonoBehaviour
{
    [Header("HUD Pages")]
    public GameObject monitoringPage;
    public GameObject missionPlanningPage;
    public GameObject missionProgressPage;

    void Start()
    {
        ShowMonitoring();
    }

    public void ShowMonitoring()
    {
        ShowPage(monitoringPage);
    }

    public void ShowPlanning()
    {
        ShowPage(missionPlanningPage);
    }

    public void ShowProgress()
    {
        ShowPage(missionProgressPage);
    }

    private void ShowPage(GameObject selectedPage)
    {
        if (monitoringPage != null)
            monitoringPage.SetActive(
                monitoringPage == selectedPage
            );

        if (missionPlanningPage != null)
            missionPlanningPage.SetActive(
                missionPlanningPage == selectedPage
            );

        if (missionProgressPage != null)
            missionProgressPage.SetActive(
                missionProgressPage == selectedPage
            );
    }
}
