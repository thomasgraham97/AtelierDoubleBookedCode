using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class menu : MonoBehaviour 
{
	public Transform viewer;
	public TextMesh timerMesh;

	bool countingDown; float countdown = 5f;

	RaycastHit hit;

	void Start()
	{
		timerMesh.gameObject.SetActive(false);
	}

	void Update()
	{
		if ( Physics.Raycast ( viewer.position, viewer.forward, out hit, 1000f ) )
		{
			if (hit.collider.tag == "start") 
			{ 
				if ( countingDown )
				{
					if ( countdown <= 0 ) { SceneManager.LoadScene(1); }
					timerMesh.text = Mathf.Ceil ( countdown ).ToString();
					countdown -= Time.deltaTime;
				}
				else { timerMesh.gameObject.SetActive(true); timerMesh.text = "5"; countingDown = true; }
			}
		}
		else if ( countingDown )
		{
			countingDown = false; countdown = 5f;
			timerMesh.gameObject.SetActive(false);
		}
	}
}
