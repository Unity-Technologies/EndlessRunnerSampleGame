using UnityEngine;

/// <summary>
/// This class is used to modify the game state (e.g. limit length run, seed etc.)
/// Subclass it and override wanted messages to handle the state.
/// </summary>
public class Modifier
{
	public virtual void OnRunStart(GameState state)
	{

	}

	public virtual void OnRunTick(GameState state)
	{

	}

	//return true if the gameobver screen should be displayed, returning false will return directly to loadout (useful for challenge)
	public virtual bool OnRunEnd(GameState state)
	{
		return true;
	}
}

// The following classes are all the samples modifiers.

public class LimitedLengthRun : Modifier
{
	public float distance;

	public LimitedLengthRun(float dist)
	{
		distance = dist;
	}

	public override void OnRunTick(GameState state)
	{
		if(state.trackManager.worldDistance >= distance)
		{
			state.trackManager.characterController.currentLife = 0;
		}
	}

	public override void OnRunStart(GameState state)
	{

	}

	public override bool OnRunEnd(GameState state)
	{
		state.QuitToLoadout();
		return false;
	}
}

public class SeededRun : Modifier
{
	int m_Seed;

    protected const int k_DaysInAWeek = 7;

	public SeededRun()
	{
        m_Seed = System.DateTime.Now.DayOfYear / k_DaysInAWeek;
	}

	public override void OnRunStart(GameState state)
	{
		state.trackManager.trackSeed = m_Seed;
	}

	public override bool OnRunEnd(GameState state)
	{
		state.QuitToLoadout();
		return false;
	}
}

public class SingleLifeRun : Modifier
{
	public override void OnRunTick(GameState state)
	{
		if (state.trackManager.characterController.currentLife > 1)
			state.trackManager.characterController.currentLife = 1;
	}


	public override void OnRunStart(GameState state)
	{

	}

	public override bool OnRunEnd(GameState state)
	{
		state.QuitToLoadout();
		return false;
	}
}
