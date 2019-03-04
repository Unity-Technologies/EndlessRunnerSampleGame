using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AllLaneObstacle: Obstacle
{
	public override IEnumerator Spawn(TrackSegment segment, float t)
	{
		Vector3 position;
		Quaternion rotation;
		segment.GetPointAt(t, out position, out rotation);
	    IAsyncOperation op = Addressables.Instantiate(gameObject.name, position, rotation);
	    yield return op;
	    GameObject obj = op.Result as GameObject;

	    if (obj == null)
	        Debug.Log(gameObject.name);
	    else
	    {
	        obj.transform.SetParent(segment.objectRoot, true);

	        //TODO : remove that hack related to #issue7
	        Vector3 oldPos = obj.transform.position;
	        obj.transform.position += Vector3.back;
	        obj.transform.position = oldPos;
	    }
	}
}
