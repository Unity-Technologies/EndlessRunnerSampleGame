using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif
#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

public class ShopUI : MonoBehaviour
{
    public ConsumableDatabase consumableDatabase;

    public ShopItemList itemList;
    public ShopCharacterList characterList;
    public ShopAccessoriesList accessoriesList;
    public ShopThemeList themeList;

    [Header("UI")]
    public Text coinCounter;
    public Text premiumCounter;
    public Button cheatButton;

    protected ShopList m_OpenList;

    protected const int k_CheatCoins = 1000000;
    protected const int k_CheatPremium = 1000;
#if UNITY_ADS
    protected const int k_AdRewardCoins = 100;
#endif

	void Start ()
    {
        PlayerData.Create();

        consumableDatabase.Load();
        CoroutineHandler.StartStaticCoroutine(CharacterDatabase.LoadDatabase());
        CoroutineHandler.StartStaticCoroutine(ThemeDatabase.LoadDatabase());


#if UNITY_ANALYTICS
        AnalyticsEvent.StoreOpened(StoreType.Soft);
#endif

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        //Disable cheating on non dev build outside of the editor
        cheatButton.interactable = false;
#else
        cheatButton.interactable = true;
#endif

        m_OpenList = itemList;
        itemList.Open();
	}
	
	void Update ()
    {
        coinCounter.text = PlayerData.instance.coins.ToString();
        premiumCounter.text = PlayerData.instance.premium.ToString();
    }

    public void OpenItemList()
    {
        m_OpenList.Close();
        itemList.Open();
        m_OpenList = itemList;
    }

    public void OpenCharacterList()
    {
        m_OpenList.Close();
        characterList.Open();
        m_OpenList = characterList;
    }

    public void OpenThemeList()
    {
        m_OpenList.Close();
        themeList.Open();
        m_OpenList = themeList;
    }

    public void OpenAccessoriesList()
    {
        m_OpenList.Close();
        accessoriesList.Open();
        m_OpenList = accessoriesList;
    }

    public void LoadScene(string scene)
    {
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }

	public void CloseScene()
	{
        SceneManager.UnloadSceneAsync("shop");
	    LoadoutState loadoutState = GameManager.instance.topState as LoadoutState;
	    if(loadoutState != null)
        {
            loadoutState.Refresh();
        }
	}

	public void CheatCoin()
	{
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        return ; //you can't cheat in production build
#endif

        PlayerData.instance.coins += k_CheatCoins;
		PlayerData.instance.premium += k_CheatPremium;
		PlayerData.instance.Save();
	}

#if UNITY_ADS
    public void ShowRewardedAd()
    {
        if (Advertisement.IsReady("rewardedVideo"))
        {
            var options = new ShowOptions { resultCallback = HandleShowResult };
            Advertisement.Show("rewardedVideo", options);
        }
    }

    private void HandleShowResult(ShowResult result)
    {
        switch (result)
        {
            case ShowResult.Finished:
                Debug.Log("The ad was successfully shown.");
                PlayerData.instance.coins += k_AdRewardCoins;
                PlayerData.instance.Save();
                break;
            case ShowResult.Skipped:
                Debug.Log("The ad was skipped before reaching the end.");
                break;
            case ShowResult.Failed:
                Debug.LogError("The ad failed to be shown.");
                break;
        }
    }
#endif
}
