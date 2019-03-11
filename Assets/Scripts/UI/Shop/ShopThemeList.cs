using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

public class ShopThemeList : ShopList
{
    public override void Populate()
    {
		m_RefreshCallback = null;
        foreach (Transform t in listRoot)
        {
            Destroy(t.gameObject);
        }

        foreach (KeyValuePair<string, ThemeData> pair in ThemeDatabase.dictionnary)
        {
            ThemeData theme = pair.Value;
            if (theme != null)
            {
                prefabItem.Instantiate().Completed += (op) =>
                {
                    GameObject newEntry = op.Result;
                    newEntry.transform.SetParent(listRoot, false);

                    ShopItemListItem itm = newEntry.GetComponent<ShopItemListItem>();

                    itm.nameText.text = theme.themeName;
                    itm.pricetext.text = theme.cost.ToString();
                    itm.icon.sprite = theme.themeIcon;

                    if (theme.premiumCost > 0)
                    {
                        itm.premiumText.transform.parent.gameObject.SetActive(true);
                        itm.premiumText.text = theme.premiumCost.ToString();
                    }
                    else
                    {
                        itm.premiumText.transform.parent.gameObject.SetActive(false);
                    }

                    itm.buyButton.onClick.AddListener(delegate() { Buy(theme); });

                    itm.buyButton.image.sprite = itm.buyButtonSprite;

                    RefreshButton(itm, theme);
                    m_RefreshCallback += delegate() { RefreshButton(itm, theme); };
                };
            }
        }
    }

	protected void RefreshButton(ShopItemListItem itm, ThemeData theme)
	{
		if (theme.cost > PlayerData.instance.coins)
		{
			itm.buyButton.interactable = false;
			itm.pricetext.color = Color.red;
		}
		else
		{
			itm.pricetext.color = Color.black;
		}

		if (theme.premiumCost > PlayerData.instance.premium)
		{
			itm.buyButton.interactable = false;
			itm.premiumText.color = Color.red;
		}
		else
		{
			itm.premiumText.color = Color.black;
		}

		if (PlayerData.instance.themes.Contains(theme.themeName))
		{
			itm.buyButton.interactable = false;
			itm.buyButton.image.sprite = itm.disabledButtonSprite;
			itm.buyButton.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Owned";
		}
	}


	public void Buy(ThemeData t)
    {
        PlayerData.instance.coins -= t.cost;
		PlayerData.instance.premium -= t.premiumCost;
        PlayerData.instance.AddTheme(t.themeName);
        PlayerData.instance.Save();

#if UNITY_ANALYTICS // Using Analytics Standard Events v0.3.0
        var transactionId = System.Guid.NewGuid().ToString();
        var transactionContext = "store";
        var level = PlayerData.instance.rank.ToString();
        var itemId = t.themeName;
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
        
        if (t.cost > 0)
        {
            AnalyticsEvent.ItemSpent(
                AcquisitionType.Soft, // Currency type
                transactionContext,
                t.cost,
                itemId,
                PlayerData.instance.coins, // Balance
                itemType,
                level,
                transactionId
            );
        }

        if (t.premiumCost > 0)
        {
            AnalyticsEvent.ItemSpent(
                AcquisitionType.Premium, // Currency type
                transactionContext,
                t.premiumCost,
                itemId,
                PlayerData.instance.premium, // Balance
                itemType,
                level,
                transactionId
            );
        }
#endif

        // Repopulate to change button accordingly.
        Populate();
    }
}
