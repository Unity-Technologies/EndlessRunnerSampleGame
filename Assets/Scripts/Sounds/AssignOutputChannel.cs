using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

//This is a helper script that allow to assign an object in a bundle it's mixer output to the main mixer
//as if we assign it in editor, a copy of the mixer will be added to the bundle and won't use the main mixer
public class AssignOutputChannel : MonoBehaviour
{
    public string mixerGroup;

    private void Awake()
    {
        AudioSource source = GetComponent<AudioSource>();

        if (source == null)
        {
            Debug.LogError("That object don't have any audio source, can't change it's output", gameObject);
            Destroy(this);
            return;
        }

        AudioMixerGroup[] groups = MusicPlayer.instance.mixer.FindMatchingGroups(mixerGroup);

        if(groups.Length == 0)
        {
            Debug.LogErrorFormat(gameObject, "Could not find any group called {0}", mixerGroup);
        }

        for(int i = 0; i < groups.Length; ++i)
        {
            if (groups[i].name == mixerGroup)
            {
                source.outputAudioMixerGroup = groups[i];
                break;
            }
        }
    }
}
