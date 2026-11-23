using UnityEngine;
using System.Collections.Generic;
using CesiumForUnity;
using Unity.Mathematics;


[RequireComponent(typeof(LineRenderer))]
public class LunarPathVisualizer : MonoBehaviour

{

public CesiumGeoreference moonGeoreference;

private LineRenderer lineRenderer;

private void Awake()
    {
        
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.startWidth = 0.3f;

        lineRenderer.endWidth = 0.3f;

        lineRenderer.useWorldSpace = true;

    }

private Vector3 ConvertToUnityPosition(LunarNode node)
    {
        double3 moonPosition = 
            moonGeoreference.ellipsoid.LongitudeLatitudeHeightToCenteredFixed(node.position);

        double3 unityPosition =
            moonGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(moonPosition);

        Vector3 localPosition = new Vector3(

            (float)unityPosition.x,
            (float)unityPosition.y,
            (float)unityPosition.z


        );

        return moonGeoreference.transform.TransformPoint(localPosition);
    }

public void DrawPath(List<LunarNode> path)
    {
        
        if(path == null || path.Count == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = path.Count;

        for (int i = 0; i< path.Count ; i++)
        {
            Vector3 worldPosition = ConvertToUnityPosition(path[i]);

            lineRenderer.SetPosition(i, worldPosition);
        }

    }


}
