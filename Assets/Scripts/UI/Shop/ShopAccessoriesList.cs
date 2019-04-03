using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

public class ShopAccessoriesList : ShopList
{
    public AssetReference headerPrefab;

    List<Character> m_CharacterList = new List<Character>();
    public override void Populate()
    {
		m_RefreshCallback = null;

        foreach (Transform t in listRoot)
        {
            Destroy(t.gameObject);
        }

        m_CharacterList.Clear();
        foreach (KeyValuePair<string, Character> pair in CharacterDatabase.dictionary)
        {
            Character c = pair.Value;
            
            if (c.accessories !=null && c.accessories.Length > 0)
                m_CharacterList.Add(c);
        }

        headerPrefab.Instantiate().Completed += (op) =>
        {
            LoadedCharacter(op, 0);
        };
    }

    void LoadedCharacter(IAsyncOperation<GameObject> op, int currentIndex)
    {
        if (op.Result == null || !(op.Result is GameObject))
        {
            Debug.LogWarning(string.Format("Unable to load header {0}.", headerPrefab.Asset.name));
        }
        else
        {
            Character c = m_CharacterList[currentIndex];
            
            GameObject header = op.Result;
            header.transform.SetParent(listRoot, false);
            ShopItemListItem itmHeader = header.GetComponent<ShopItemListItem>();
            itmHeader.nameText.text = c.characterName;

            prefabItem.Instantiate().Completed += (innerOp) =>
            {
	            LoadedAccessory(innerOp, currentIndex, 0);
            };
        }
    }

    void LoadedAccessory(IAsyncOperation<GameObject> op, int characterIndex, int accessoryIndex)
    {
	    Character c = m_CharacterList[characterIndex];
	    if (op.Result == null || !(op.Result is GameObject))
	    {
		    Debug.LogWarning(string.Format("Unable to load shop accessory list {0}.", prefabItem.Asset.name));
	    }
	    else
	    {
		    CharacterAccessories accessory = c.accessories[accessoryIndex];

		    GameObject newEntry = op.Result;
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
	    }

	    accessoryIndex++;
	    
	    if (accessoryIndex == c.accessories.Length)
	    {//we finish the current character accessory, load the next character

		    characterIndex++;
		    if (characterIndex < m_CharacterList.Count)
		    {
			    headerPrefab.Instantiate().Completed += (innerOp) =>
			    {
				    LoadedCharacter(innerOp, characterIndex);
			    };
		    }
	    }
	    else
	    {
		    prefabItem.Instantiate().Completed += (innerOp) =>
		    {
			    LoadedAccessory(innerOp, characterIndex, accessoryIndex);
		    };
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
