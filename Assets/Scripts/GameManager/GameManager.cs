using UnityEngine;
using System.Collections.Generic;
#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

/// <summary>
/// The Game manager is a state machine, that will switch between state according to current gamestate.
/// </summary>
public class GameManager : MonoBehaviour
{
    static public GameManager instance { get { return s_Instance; } }
    static protected GameManager s_Instance;

    public AState[] states;
    public AState topState {  get { if (m_StateStack.Count == 0) return null; return m_StateStack[m_StateStack.Count - 1]; } }

    public ConsumableDatabase m_ConsumableDatabase;

    protected List<AState> m_StateStack = new List<AState>();
    protected Dictionary<string, AState> m_StateDict = new Dictionary<string, AState>();

    protected void OnEnable()
    {
        PlayerData.Create();

        s_Instance = this;

        m_ConsumableDatabase.Load();

        // We build a dictionnary from state for easy switching using their name.
        m_StateDict.Clear();

        if (states.Length == 0)
            return;

        for(int i = 0; i < states.Length; ++i)
        {
            states[i].manager = this;
            m_StateDict.Add(states[i].GetName(), states[i]);
        }

        m_StateStack.Clear();

        PushState(states[0].GetName());
    }

    protected void Update()
    {
        if(m_StateStack.Count > 0)
        {
            m_StateStack[m_StateStack.Count - 1].Tick();
        }
    }

    protected void OnApplicationQuit()
    {
#if UNITY_ANALYTICS
        // We are exiting during game, so this make this invalid, send an event to log it
        // NOTE : this is only called on standalone build, as on mobile this function isn't called
        bool inGameExit = m_StateStack[m_StateStack.Count - 1].GetType() == typeof(GameState);

        Analytics.CustomEvent("user_end_session", new Dictionary<string, object>
        {
            { "force_exit", inGameExit },
            { "timer", Time.realtimeSinceStartup }
        });
#endif
    }

    // State management
    public void SwitchState(string newState)
    {
        AState state = FindState(newState);
        if (state == null)
        {
            Debug.LogError("Can't find the state named " + newState);
            return;
        }

        m_StateStack[m_StateStack.Count - 1].Exit(state);
        state.Enter(m_StateStack[m_StateStack.Count - 1]);
        m_StateStack.RemoveAt(m_StateStack.Count - 1);
        m_StateStack.Add(state);
    }

	public AState FindState(string stateName)
	{
		AState state;
		if (!m_StateDict.TryGetValue(stateName, out state))
		{
			return null;
		}

		return state;
	}

    public void PopState()
    {
        if(m_StateStack.Count < 2)
        {
            Debug.LogError("Can't pop states, only one in stack.");
            return;
        }

        m_StateStack[m_StateStack.Count - 1].Exit(m_StateStack[m_StateStack.Count - 2]);
        m_StateStack[m_StateStack.Count - 2].Enter(m_StateStack[m_StateStack.Count - 2]);
        m_StateStack.RemoveAt(m_StateStack.Count - 1);
    }

    public void PushState(string name)
    {
        AState state;
        if(!m_StateDict.TryGetValue(name, out state))
        {
            Debug.LogError("Can't find the state named " + name);
            return;
        }

        if (m_StateStack.Count > 0)
        {
            m_StateStack[m_StateStack.Count - 1].Exit(state);
            state.Enter(m_StateStack[m_StateStack.Count - 1]);
        }
        else
        {
            state.Enter(null);
        }
        m_StateStack.Add(state);
    }
}

public abstract class AState : MonoBehaviour
{
    [HideInInspector]
    public GameManager manager;

    public abstract void Enter(AState from);
    public abstract void Exit(AState to);
    public abstract void Tick();

    public abstract string GetName();
}