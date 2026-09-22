using UnityEngine;

public class LicenceDisplayer : MonoBehaviour
{
    void Start ()
    {
        PlayerData.Create();

	    if(PlayerData.instance.licenceAccepted)
        {
            // If we have already accepted the licence, we close the popup, no need for it.
            Close();
        }	
	}
	
	public void Accepted()
    {
        PlayerData.instance.licenceAccepted = true;
        PlayerData.instance.Save();
        Close();
    }

    public void Refuse()
    {
        Application.Quit();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
