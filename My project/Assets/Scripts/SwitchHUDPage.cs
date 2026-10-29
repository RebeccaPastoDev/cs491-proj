using TMPro;
using UnityEngine;

public class SwitchHUDPage : MonoBehaviour
{
    public GameObject monitoringBar;
    public GameObject missionPlanningBar;
    public TMP_Text pageSwitchText;

    private bool showingMonitoring = true;

    void Start()
    {
        UpdatePage();
    }

    public void SwitchPage()
    {
        showingMonitoring = !showingMonitoring;
        UpdatePage();
    }

    void UpdatePage()
    {
        monitoringBar.SetActive(showingMonitoring);
        missionPlanningBar.SetActive(!showingMonitoring);

        pageSwitchText.text = showingMonitoring
            ? "MISSION PLANNING →"
            : "← BACK TO MONITORING";
    }
}
