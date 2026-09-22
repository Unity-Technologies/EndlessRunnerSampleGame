using UnityEngine;

public class DataDeleteConfirmation : MonoBehaviour
{
    protected LoadoutState m_LoadoutState;

    public void Open(LoadoutState owner)
    {
        gameObject.SetActive(true);
        m_LoadoutState = owner;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Confirm()
    {
        PlayerData.NewSave();
        m_LoadoutState.UnequipPowerup();
        m_LoadoutState.Refresh();
        Close();
    }

    public void Deny()
    {
        Close();
    }
}
