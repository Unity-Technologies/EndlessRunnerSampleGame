using UnityEngine;
using UnityEngine.AddressableAssets;

// Base class for any list in the shop (Consumable, Character, Themes)
public abstract class ShopList : MonoBehaviour
{
    public AssetReference prefabItem;
    public RectTransform listRoot;

	public delegate void RefreshCallback();

	protected RefreshCallback m_RefreshCallback;

    public void Open()
    {
        Populate();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        m_RefreshCallback = null;
    }

	public void Refresh()
	{
		m_RefreshCallback();
	}

    public abstract void Populate();
}
