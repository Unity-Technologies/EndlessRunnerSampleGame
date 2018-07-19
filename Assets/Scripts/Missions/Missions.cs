using System;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Base abstract class used to define a mission the player needs to complete to gain some premium currency.
/// Subclassed for every mission.
/// </summary>
public abstract class MissionBase
{
    // Mission type
    public enum MissionType
    {
        SINGLE_RUN,
        PICKUP,
        OBSTACLE_JUMP,
        SLIDING,
        MULTIPLIER,
        MAX
    }

    public float progress;
    public float max;
    public int reward;

    public bool isComplete { get { return (progress / max) >= 1.0f; } }

    public void Serialize(BinaryWriter w)
    {
        w.Write(progress);
        w.Write(max);
        w.Write(reward);
    } 

    public void Deserialize(BinaryReader r)
    {
        progress = r.ReadSingle();
        max = r.ReadSingle();
        reward = r.ReadInt32();
    }

	public virtual bool HaveProgressBar() { return true; }

    public abstract void Created();
    public abstract MissionType GetMissionType();
    public abstract string GetMissionDesc();
    public abstract void RunStart(TrackManager manager);
    public abstract void Update(TrackManager manager);

    static public MissionBase GetNewMissionFromType(MissionType type)
    {
        switch (type)
        {
            case MissionType.SINGLE_RUN:
                return new SingleRunMission();
            case MissionType.PICKUP:
                return new PickupMission();
            case MissionType.OBSTACLE_JUMP:
                return new BarrierJumpMission();
            case MissionType.SLIDING:
                return new SlidingMission();
            case MissionType.MULTIPLIER:
                return new MultiplierMission();
        }

        return null;
    }
}

public class SingleRunMission : MissionBase
{
    public override void Created()
    {
        float[] maxValues = { 500, 1000, 1500, 2000 };
        int choosenVal = Random.Range(0, maxValues.Length);

        reward = choosenVal + 1;
        max = maxValues[choosenVal];
        progress = 0;
    }

	public override bool HaveProgressBar()
	{
		return false;
	}

	public override string GetMissionDesc()
    {
        return "Run " + ((int)max) + "m in a single run";
    }

    public override MissionType GetMissionType()
    {
        return MissionType.SINGLE_RUN;
    }

    public override void RunStart(TrackManager manager)
    {
        progress = 0;
    }

    public override void Update(TrackManager manager)
    {
        progress = manager.worldDistance;
    }
}

public class PickupMission : MissionBase
{
    int previousCoinAmount;

    public override void Created()
    {
        float[] maxValues = { 1000, 2000, 3000, 4000 };
        int choosen = Random.Range(0, maxValues.Length);

        max = maxValues[choosen];
        reward = choosen + 1;
        progress = 0;
    }

    public override string GetMissionDesc()
    {
        return "Pickup " + max + " fishbones";
    }

    public override MissionType GetMissionType()
    {
        return MissionType.PICKUP;
    }

    public override void RunStart(TrackManager manager)
    {
        previousCoinAmount = 0;
    }

    public override void Update(TrackManager manager)
    {
        int coins = manager.characterController.coins - previousCoinAmount;
        progress += coins;

        previousCoinAmount = manager.characterController.coins;
    }
}

public class BarrierJumpMission : MissionBase
{
    Obstacle m_Previous;
    Collider[] m_Hits;

    protected const int k_HitColliderCount = 8;
    protected readonly Vector3 k_CharacterColliderSizeOffset = new Vector3(-0.3f, 2f, -0.3f);
    
    public override void Created()
    {
        float[] maxValues = { 20, 50, 75, 100 };
        int choosen = Random.Range(0, maxValues.Length);

        max = maxValues[choosen];
        reward = choosen + 1;
        progress = 0;
    }

    public override string GetMissionDesc()
    {
        return "Jump over " + ((int)max) + " barriers";
    }

    public override MissionType GetMissionType()
    {
        return MissionType.OBSTACLE_JUMP;
    }

    public override void RunStart(TrackManager manager)
    {
        m_Previous = null;
        m_Hits = new Collider[k_HitColliderCount];
    }

    public override void Update(TrackManager manager)
    {
        if(manager.characterController.isJumping)
        {
            Vector3 boxSize = manager.characterController.characterCollider.collider.size + k_CharacterColliderSizeOffset;
            Vector3 boxCenter = manager.characterController.transform.position - Vector3.up * boxSize.y * 0.5f;

            int count = Physics.OverlapBoxNonAlloc(boxCenter, boxSize * 0.5f, m_Hits);

            for(int i = 0; i < count; ++i)
            {
                Obstacle obs = m_Hits[i].GetComponent<Obstacle>();

                if(obs != null && obs is AllLaneObstacle)
                {
                    if(obs != m_Previous)
                    {
                        progress += 1;
                    }

                    m_Previous = obs;
                }
            }
        }
    }
}

public class SlidingMission : MissionBase
{
    float m_PreviousWorldDist;

    public override void Created()
    {
        float[] maxValues = { 20, 30, 75, 150};
        int choosen = Random.Range(0, maxValues.Length);

        reward = choosen + 1;
        max = maxValues[choosen];
        progress = 0;
    }

    public override string GetMissionDesc()
    {
        return "Slide for " + ((int)max) + "m";
    }

    public override MissionType GetMissionType()
    {
        return MissionType.SLIDING;
    }

    public override void RunStart(TrackManager manager)
    {
        m_PreviousWorldDist = manager.worldDistance;
    }

    public override void Update(TrackManager manager)
    {
        if(manager.characterController.isSliding)
        {
            float dist = manager.worldDistance - m_PreviousWorldDist;
            progress += dist;
        }

        m_PreviousWorldDist = manager.worldDistance;
    }
}

public class MultiplierMission : MissionBase
{
	public override bool HaveProgressBar()
	{
		return false;
	}

	public override void Created()
    {
        float[] maxValue = { 3, 5, 8, 10 };
        int choosen = Random.Range(0, maxValue.Length);

        max = maxValue[choosen];
        reward = (choosen + 1);

        progress = 0;
    }

    public override string GetMissionDesc()
    {
        return "Reach a x" + ((int)max) + " multiplier";
    }

    public override MissionType GetMissionType()
    {
        return MissionType.MULTIPLIER;
    }

    public override void RunStart(TrackManager manager)
    {
        progress = 0;
    }

    public override void Update(TrackManager manager)
    {
        if (manager.multiplier > progress)
            progress = manager.multiplier;
    }
}