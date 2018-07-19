using UnityEngine;

public class AllLaneObstacle: Obstacle
{
	public override void Spawn(TrackSegment segment, float t)
	{
		Vector3 position;
		Quaternion rotation;
		segment.GetPointAt(t, out position, out rotation);
		GameObject obj = Instantiate(gameObject, position, rotation);
		obj.transform.SetParent(segment.objectRoot, true);

	    //TODO : remove that hack related to #issue7
        Vector3 oldPos = obj.transform.position;
	    obj.transform.position += Vector3.back;
	    obj.transform.position = oldPos;
    }
}
