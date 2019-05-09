using System.Collections;
using UnityEngine;

/// <summary>
/// This script is the base class for implemented obstacles.
/// Derived classes should take care of spawning any object needed for the obstacles.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public abstract class Obstacle : MonoBehaviour
{
	public AudioClip impactedSound;

    public virtual void Setup() {}

    public abstract IEnumerator Spawn(TrackSegment segment, float t);

	public virtual void Impacted()
	{
		Animation anim = GetComponentInChildren<Animation>();
		AudioSource audioSource = GetComponent<AudioSource>();

		if (anim != null)
		{
			anim.Play();
		}

		if (audioSource != null && impactedSound != null)
		{
			audioSource.Stop();
			audioSource.loop = false;
			audioSource.clip = impactedSound;
			audioSource.Play();
		}
	}
}
