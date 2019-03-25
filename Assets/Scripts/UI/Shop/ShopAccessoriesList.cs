using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

public class ShopAccessoriesList : ShopList
{
    public AssetReference headerPrefab;

    public override void Populate()
    {
		m_RefreshCallback = null;

        foreach (Transform t in listRoot)
        {
            Destroy(t.gameObject);
        }

        foreach (KeyValuePair<string, Character> pair in CharacterDatabase.dictionary)
        {
            Character c = pair.Value;
            if (c != null && c.accessories !=null && c.accessories.Length > 0)
            {

                headerPrefab.Instantiate().Completed += (op) =>
                {
                    if (op.Result == null || !(op.Result is GameObject))
                    {
                        Debug.LogWarning(string.Format("Unable to load header {0}.", headerPrefab.Asset.name));
                        return;
                    }
                    GameObject header = op.Result;
                    header.transform.SetParent(listRoot, false);
                    ShopItemListItem itmHeader = header.GetComponent<ShopItemListItem>();
                    itmHeader.nameText.text = c.characterName;

                    for (int i = 0; i < c.accessories.Length; ++i)
                    {
                        CharacterAccessories accessory = c.accessories[i];
                        prefabItem.Instantiate().Completed += (innerOp) =>
                        {
                            if (op.Result == null || !(op.Result is GameObject))
                            {
                                Debug.LogWarning(string.Format("Unable to load shop accessory list {0}.", prefabItem.Asset.name));
                                return;
                            }
                            GameObject newEntry = innerOp.Result;
                            newEntry.transform.SetParent(listRoot, false);

                            ShopItemListItem itm = newEntry.GetComponent<ShopItemListItem>();

                            string compoundName = c.characterName + ":" + accessory.accessoryName;

                            itm.nameText.text = accessory.accessoryName;
                            itm.pricetext.text = accessory.cost.ToString();
                            itm.icon.sprite = accessory.accessoryIcon;
                            itm.buyButton.image.sprite = itm.buyButtonSprite;

                            if (accessory.premiumCost > 0)
                            {
                                itm.premiumText.transform.parent.gameObject.SetActive(true);
                                itm.premiumText.text = accessory.premiumCost.ToString();
                            }
                            else
                            {
                                itm.premiumText.transform.parent.gameObject.SetActive(false);
                            }

                            itm.buyButton.onClick.AddListener(delegate()
                            {
                                Buy(compoundName, accessory.cost, accessory.premiumCost);
                            });

                            m_RefreshCallback += delegate() { RefreshButton(itm, accessory, compoundName); };
                            RefreshButton(itm, accessory, compoundName);
                        };
                    }
                };
            }
        }
    }

	protected void RefreshButton(ShopItemListItem itm, CharacterAccessories accessory, string compoundName)
	{
		if (accessory.cost > PlayerData.instance.coins)
		{
			itm.buyButton.interactable = false;
			itm.pricetext.color = Color.red;
		}
		else
		{
			itm.pricetext.color = Color.black;
		}

		if (accessory.premiumCost > PlayerData.instance.premium)
		{
			itm.buyButton.interactable = false;
			itm.premiumText.color = Color.red;
		}
		else
		{
			itm.premiumText.color = Color.black;
		}

		if (PlayerData.instance.characterAccessories.Contains(compoundName))
		{
			itm.buyButton.interactable = false;
			itm.buyButton.image.sprite = itm.disabledButtonSprite;
			itm.buyButton.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Owned";
		}
	}



	public void Buy(string name, int cost, int premiumCost)
    {
        PlayerData.instance.coins -= cost;
		PlayerData.instance.premium -= premiumCost;
		PlayerData.instance.AddAccessory(name);
        PlayerData.instance.Save();

#if UNITY_ANALYTICS // Using Analytics Standard Events v0.3.0
        var transactionId = System.Guid.NewGuid().ToString();
        var transactionContext = "store";
        var level = PlayerData.instance.rank.ToString();
        var itemId = name;
        var itemType = "non_consumable";
        var itemQty = 1;

        AnalyticsEvent.ItemAcquired(
            AcquisitionType.Soft,
            transactionContext,
            itemQty,
            itemId,
            itemType,
            level,
            transactionId
        );
        
        if (cost > 0)
        {
            AnalyticsEvent.ItemSpent(
                AcquisitionType.Soft, // Currency type
                transactionContext,
                cost,
                itemId,
                PlayerData.instance.coins, // Balance
                itemType,
                level,
                transactionId
            );
        }

        if (premiumCost > 0)
        {
            AnalyticsEvent.ItemSpent(
                AcquisitionType.Premium, // Currency type
                transactionContext,
                premiumCost,
                itemId,
                PlayerData.instance.premium, // Balance
                itemType,
                level,
                transactionId
            );
        }
#endif

        Refresh();
    }
}
