using UnityEngine;

public class Helpers
{
    // This sets the layer of all renderer children of a given gameobject (including itself)
    // Useful to make an object display only on a single camera (e.g. character on UI cam on loadout State)
    static public void SetRendererLayerRecursive(GameObject root, int layer)
    {
        Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);

        for(int i = 0; i < rends.Length; ++i)
        {
            rends[i].gameObject.layer = layer;
        }
    }
}
