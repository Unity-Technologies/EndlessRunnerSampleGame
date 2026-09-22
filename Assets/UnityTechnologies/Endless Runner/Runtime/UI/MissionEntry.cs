using UnityEngine;
using UnityEngine.UI;

public class MissionEntry : MonoBehaviour
{
    public Text descText;
    public Text rewardText;
    public Button claimButton;
    public Text progressText;
	public Image background;

	public Color notCompletedColor;
	public Color completedColor;

    public void FillWithMission(MissionBase m, MissionUI owner)
    {
        descText.text = m.GetMissionDesc();
        rewardText.text = m.reward.ToString();

        if (m.isComplete)
        {
            claimButton.gameObject.SetActive(true);
            progressText.gameObject.SetActive(false);

			background.color = completedColor;

			progressText.color = Color.white;
			descText.color = Color.white;
			rewardText.color = Color.white;

			claimButton.onClick.AddListener(delegate { owner.Claim(m); } );
        }
        else
        {
            claimButton.gameObject.SetActive(false);
            progressText.gameObject.SetActive(true);

			background.color = notCompletedColor;

			progressText.color = Color.black;
			descText.color = completedColor;

			progressText.text = ((int)m.progress) + " / " + ((int)m.max);
        }
    }
}
