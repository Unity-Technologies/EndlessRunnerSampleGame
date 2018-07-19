using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif
#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

public class AdsForMission : MonoBehaviour
{
    public MissionUI missionUI;

    public Text newMissionText;
    public Button adsButton;
#if UNITY_ANALYTICS
    public AdvertisingNetwork adsNetwork = AdvertisingNetwork.UnityAds;
#endif
    public string adsPlacementId = "rewardedVideo";
    public bool adsRewarded = true; 

    void OnEnable ()
    {
        adsButton.gameObject.SetActive(false);
        newMissionText.gameObject.SetActive(false);

        // Only present an ad offer if less than 3 missions.
        if (PlayerData.instance.missions.Count >= 3)
        {
            return;
        }

#if UNITY_ADS
        var isReady = Advertisement.IsReady(adsPlacementId);
        if (isReady)
        {
#if UNITY_ANALYTICS
            AnalyticsEvent.AdOffer(adsRewarded, adsNetwork, adsPlacementId, new Dictionary<string, object>
            {
                { "level_index", PlayerData.instance.rank },
                { "distance", TrackManager.instance == null ? 0 : TrackManager.instance.worldDistance },
            });
#endif
        }

        newMissionText.gameObject.SetActive(isReady);
        adsButton.gameObject.SetActive(isReady);
#endif
    }

    public void ShowAds()
    {
#if UNITY_ADS
        if (Advertisement.IsReady(adsPlacementId))
        {
#if UNITY_ANALYTICS
            AnalyticsEvent.AdStart(adsRewarded, adsNetwork, adsPlacementId, new Dictionary<string, object>
            {
                { "level_index", PlayerData.instance.rank },
                { "distance", TrackManager.instance == null ? 0 : TrackManager.instance.worldDistance },
            });
#endif
            var options = new ShowOptions {resultCallback = HandleShowResult};
            Advertisement.Show(adsPlacementId, options);
        }
        else
        {
#if UNITY_ANALYTICS
            AnalyticsEvent.AdSkip(adsRewarded, adsNetwork, adsPlacementId, new Dictionary<string, object> {
                { "error", Advertisement.GetPlacementState(adsPlacementId).ToString() }
            });
#endif
        }
#endif
    }

#if UNITY_ADS

    private void HandleShowResult(ShowResult result)
    {
        switch (result)
        {
            case ShowResult.Finished:
                AddNewMission();
#if UNITY_ANALYTICS
                AnalyticsEvent.AdComplete(adsRewarded, adsNetwork, adsPlacementId);
#endif
                break;
            case ShowResult.Skipped:
                Debug.Log("The ad was skipped before reaching the end.");
#if UNITY_ANALYTICS
                AnalyticsEvent.AdSkip(adsRewarded, adsNetwork, adsPlacementId);
#endif
                break;
            case ShowResult.Failed:
                Debug.LogError("The ad failed to be shown.");
#if UNITY_ANALYTICS
                AnalyticsEvent.AdSkip(adsRewarded, adsNetwork, adsPlacementId, new Dictionary<string, object> {
                    { "error", "failed" }
                });
#endif
                break;
        }
    }
#endif

    void AddNewMission()
    {
        PlayerData.instance.AddMission();
        PlayerData.instance.Save();
        missionUI.Open();
    }
}
