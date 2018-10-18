using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class displayReport : MonoBehaviour 
{
	offscreen singleton;

	public GameObject card, landscapeGrade, portraitGrade;
	Material landscapeGradeMat, portraitGradeMat;

	public Texture a, b, c, d, f;

	bool beginTimer;
	float timer;

	public float animStartTime, animEndTime;
	public Vector3 cardOffset;

	bool lerpStarted, lerpFinished;
	float lerpInterval, lerpTimer;

	float landscapeGradePoint, portraitGradePoint;

	void Start()
	{
		card.transform.localPosition = cardOffset;
		card.SetActive(false);

		landscapeGradeMat = landscapeGrade.GetComponent<MeshRenderer>().material;
		portraitGradeMat = portraitGrade.GetComponent<MeshRenderer>().material;

		lerpInterval = animEndTime - animStartTime;
	}

	void Update()
	{
		if ( singleton == null ) { singleton = offscreen.singleton; }

		if ( !beginTimer && singleton.currentTake.name == "end" ) {	beginTimer = true; }
		if ( beginTimer ) 
		{
			if ( timer > animStartTime )
			{
				if ( !lerpStarted && !lerpFinished )
				{
					portraitGradePoint = singleton.takes[1].gazeZones[0].time / 120f;
					landscapeGradePoint = singleton.takes[1].gazeZones[1].time / 120f;

					if ( portraitGradePoint < 0.5f ) { portraitGradeMat.SetTexture ( "_MainTex", f ); }
					else if ( portraitGradePoint >= 0.5f && portraitGradePoint < 0.6f )
						{ portraitGradeMat.SetTexture ( "_MainTex", d ); }
					else if ( portraitGradePoint >= 0.6f && portraitGradePoint < 0.7f )
						{ portraitGradeMat.SetTexture ( "_MainTex", c ); }
					else if ( portraitGradePoint >= 0.7f && portraitGradePoint < 0.8f )
						{ portraitGradeMat.SetTexture ( "_MainTex", b ); }

					landscapeGrade.transform.localPosition += Vector3.Scale ( Random.insideUnitSphere, new Vector3 ( 0.01f, 0.01f, 0f ) );
					portraitGrade.transform.localPosition += Vector3.Scale ( Random.insideUnitSphere, new Vector3 ( 0.01f, 0.01f, 0f ) );

					landscapeGrade.transform.localScale = landscapeGrade.transform.localScale * Random.Range (0.95f, 1.05f);
					portraitGrade.transform.localScale = landscapeGrade.transform.localScale * Random.Range (0.95f, 1.05f);

					landscapeGrade.transform.Rotate ( Vector3.forward * Random.Range ( -10f, 10f ) );
					portraitGrade.transform.Rotate ( Vector3.forward * Random.Range ( -10f, 10f ) );

					card.SetActive(true);
					lerpStarted = true;
				}

				else 
				{
					card.transform.localPosition = Vector3.Slerp ( cardOffset, Vector3.zero, lerpTimer / lerpInterval );
					lerpTimer += Time.deltaTime;

					if (lerpTimer > lerpInterval ) { lerpFinished = true; }
				}
			}

			timer += Time.deltaTime;
		}

		if ( lerpFinished && timer > 25f )
			{ UnityEngine.SceneManagement.SceneManager.LoadScene (0); }
	}
}
