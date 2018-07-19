using UnityEngine;
using UnityEngine.UI;

public class PowerupIcon : MonoBehaviour
{
    [HideInInspector]
    public Consumable linkedConsumable;

    public Image icon;
    public Slider slider;

	void Start ()
    { 
        icon.sprite = linkedConsumable.icon;
	}

    void Update()
    {
        slider.value = 1.0f - linkedConsumable.timeActive / linkedConsumable.duration;
    }
}
