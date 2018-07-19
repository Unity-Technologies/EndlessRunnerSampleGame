using System.Collections.Generic;
using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif
#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

public class IAPHandler : MonoBehaviour
{
#if UNITY_PURCHASING
    private void OnEnable()
    {
#if UNITY_ANALYTICS
        AnalyticsEvent.StoreOpened(StoreType.Premium);
#endif
    }

    public void ProductBought(Product product)
    {
        int amount = 0;
        switch (product.definition.id)
        {
            case "10_premium":
                amount = 10;
                break;
            case "50_premium":
                amount = 50;
                break;
            case "100_premium":
                amount = 100;
                break;
        }

        if (amount > 0)
        {
            PlayerData.instance.premium += amount;
            PlayerData.instance.Save();

#if UNITY_ANALYTICS // Using Analytics Standard Events v0.3.0
            var transactionId = product.transactionID;
            var transactionContext = "premium_store";
            var itemId = product.definition.id;
            var itemType = "consumable";
            var level = PlayerData.instance.rank.ToString();
            
            AnalyticsEvent.IAPTransaction(
                transactionContext,
                (float)product.metadata.localizedPrice,
                itemId,
                itemType,
                level,
                transactionId
            );
            
            AnalyticsEvent.ItemAcquired( 
                AcquisitionType.Premium, // Currency type
                transactionContext,
                amount,
                itemId,
                PlayerData.instance.premium, // Item balance
                itemType,
                level,
                transactionId
            );
#endif
        }
    }

    public void ProductError(Product product, PurchaseFailureReason reason)
    {
        Debug.LogError("Product : " + product.definition.id + " couldn't be bought because " + reason);
    }
#endif
}
