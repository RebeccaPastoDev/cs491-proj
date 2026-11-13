
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RoverWarningManager : MonoBehaviour
{
    [Header("Monitoring Scripts")]
    public RoverTerrainMonitor terrainMonitor;
    public RoverObstacleMonitor obstacleMonitor;

    [Header("Desktop System Warnings")]
    public TMP_Text systemWarningsText;

    [Header("Desktop Notification Banner")]
    public GameObject warningBanner;
    public TMP_Text warningBannerText;
    public Image warningBannerBackground;

    [Header("VR System Warnings")]
    public TMP_Text vrSystemWarningsText;

    [Header("VR Notification Banner")]
    public GameObject vrWarningBanner;
    public TMP_Text vrWarningBannerText;
    public Image vrWarningBannerBackground;

    [Header("Slope Thresholds")]
    public float cautionSlope = 10f;
    public float warningSlope = 15f;

    [Header("Obstacle Thresholds")]
    public float cautionDistance = 10f;
    public float warningDistance = 5f;

    private readonly Color normalColor =
        new Color32(119, 201, 154, 255);

    private readonly Color cautionColor =
        new Color32(255, 180, 65, 255);

    private readonly Color criticalColor =
        new Color32(255, 100, 100, 255);

    private readonly Color cautionBackground =
        new Color32(61, 43, 23, 240);

    private readonly Color criticalBackground =
        new Color32(66, 29, 35, 240);

    private struct Warning
    {
        public string message;
        public int priority;

        public Warning(string message, int priority)
        {
            this.message = message;
            this.priority = priority;
        }
    }

    void LateUpdate()
    {
        List<Warning> warnings = new List<Warning>();

        // Check terrain slope.
        if (terrainMonitor != null &&
            terrainMonitor.HasGround)
        {
            float slope = terrainMonitor.CurrentSlope;

            if (slope >= warningSlope)
            {
                warnings.Add(new Warning(
                    $"Steep terrain: {slope:F1}°", 2));
            }
            else if (slope >= cautionSlope)
            {
                warnings.Add(new Warning(
                    $"Slope caution: {slope:F1}°", 1));
            }
        }

        // Check simulated LiDAR.
        if (obstacleMonitor != null &&
            obstacleMonitor.HasObstacle)
        {
            float distance =
                obstacleMonitor.ClosestObstacleDistance;

            string direction =
                obstacleMonitor.ClosestObstacleDirection;

            if (distance <= warningDistance)
            {
                warnings.Add(new Warning(
                    $"Obstacle {direction}: {distance:F1} m", 2));
            }
            else if (distance <= cautionDistance)
            {
                warnings.Add(new Warning(
                    $"Obstacle nearby {direction}: {distance:F1} m", 1));
            }
        }

        // Critical warnings appear first.
        warnings.Sort((a, b) =>
            b.priority.CompareTo(a.priority));

        UpdateWarningDisplays(warnings);
    }

    void UpdateWarningDisplays(List<Warning> warnings)
    {
        // No hazards: hide notification banners.
        if (warnings.Count == 0)
        {
            SetSystemWarnings(
                "No active warnings",
                normalColor
            );

            SetBanner(false, "", 0);
            return;
        }

        // Show up to two warnings in Monitoring.
        string message = "";

        int count = Mathf.Min(2, warnings.Count);

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                message += "\n";

            message += "!!! " + warnings[i].message;
        }

        Color textColor = warnings[0].priority == 2
            ? criticalColor
            : cautionColor;

        SetSystemWarnings(message, textColor);

        // Display highest-priority warning in banner.
        SetBanner(
            true,
            warnings[0].message,
            warnings[0].priority
        );
    }

    void SetSystemWarnings(string message, Color color)
    {
        if (systemWarningsText != null)
        {
            systemWarningsText.text = message;
            systemWarningsText.color = color;
        }

        if (vrSystemWarningsText != null)
        {
            vrSystemWarningsText.text = message;
            vrSystemWarningsText.color = color;
        }
    }

    void SetBanner(bool visible, string message, int priority)
    {
        UpdateBanner(
            warningBanner,
            warningBannerText,
            warningBannerBackground,
            visible,
            message,
            priority
        );

        UpdateBanner(
            vrWarningBanner,
            vrWarningBannerText,
            vrWarningBannerBackground,
            visible,
            message,
            priority
        );
    }

    void UpdateBanner(
        GameObject banner,
        TMP_Text bannerText,
        Image background,
        bool visible,
        string message,
        int priority)
    {
        if (banner == null)
            return;

        if (banner.activeSelf != visible)
            banner.SetActive(visible);

        if (!visible)
            return;

        bool critical = priority == 2;

        if (bannerText != null)
        {
            string label = critical
                ? "CRITICAL"
                : "CAUTION";

            // Text-only warning style.
            bannerText.text =
                $"!!! {label}: {message} !!!";

            bannerText.color = critical
                ? criticalColor
                : cautionColor;
        }

        if (background != null)
        {
            background.color = critical
                ? criticalBackground
                : cautionBackground;
        }
    }
}
