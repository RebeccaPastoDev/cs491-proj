using UnityEngine;

public class BaseCampSize : MonoBehaviour
{
    [ContextMenu("Calculate Base Camp Size")]
    public void CalculateSize()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found in Base Camp.");
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Debug.Log(
            $"BASE CAMP SIZE\n" +
            $"Width (X): {bounds.size.x:F2} units\n" +
            $"Height (Y): {bounds.size.y:F2} units\n" +
            $"Length (Z): {bounds.size.z:F2} units"
        );
    }
}
