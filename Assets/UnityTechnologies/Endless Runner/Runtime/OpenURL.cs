using UnityEngine;

public class OpenURL : MonoBehaviour
{
    public string websiteAddress;

    public void OpenURLOnClick()
    {
        Application.OpenURL(websiteAddress);
    }
}