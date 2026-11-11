using UnityEngine;

public class BaseCampSize : MonoBehaviour
{
    [ContextMenu("Measure Base Camp")]
    public void MeasureBaseCamp()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found.");
            return;
        }

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        Debug.Log(
            $"BASE CAMP SIZE:\n" +
            $"Width (X): {bounds.size.x:F2} meters\n" +
            $"Height (Y): {bounds.size.y:F2} meters\n" +
            $"Length (Z): {bounds.size.z:F2} meters\n" +
            $"Center: {bounds.center}"
        );
    }
}
