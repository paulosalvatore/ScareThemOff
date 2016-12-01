using UnityEngine;
//using System.Collections.Generic;

public class DontDestroy : MonoBehaviour {
	void Start()
	{
		if (GameObject.FindGameObjectsWithTag(gameObject.tag).Length > 1)
			Destroy(gameObject);
		else
			DontDestroyOnLoad(gameObject);
	}
}
