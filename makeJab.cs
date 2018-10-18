using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class makeJab : MonoBehaviour 
{
	offscreen singleton;

	AudioClip[] landscapeJabs, portraitJabs;
	AudioSource landscapeMainSource, landscapeSecondarySource, portraitMainSource, portraitSecondarySource;

	int lastLandscapeJabIndex = -1, lastPortraitJabIndex = -1;
	public float minimumTimeBetweenJabs; float timeSinceLastJab;

	bool landscapePlayingJab, portraitPlayingJab;

	public bool lessonStarted, lessonConcluded;
	public float lessonEndTime = 90f;
	float lessonTimeElapsed;

	bool rampLandscapeUp, rampLandscapeDown, rampPortraitUp, rampPortraitDown;
	float lsUpFloat, lsDownFloat, ptUpFloat, ptDownFloat, rampTime = 0.5f;

	void Update()
	{
		if ( singleton == null ) { singleton = offscreen.singleton; }
		else
		{
			if ( landscapeMainSource == null )
			{
				landscapeMainSource = singleton.currentTake.actors[0].mainAudioSource;
				landscapeSecondarySource = singleton.currentTake.actors[0].secondaryAudioSource;
				landscapeJabs = singleton.currentTake.actors[0].secondaryAudioClips;

				portraitMainSource = singleton.currentTake.actors[1].mainAudioSource;
				portraitSecondarySource = singleton.currentTake.actors[1].secondaryAudioSource;
				portraitJabs = singleton.currentTake.actors[1].secondaryAudioClips;
			}
			else
			{
				if ( lessonStarted && lessonTimeElapsed < lessonEndTime )
				{
					if ( landscapePlayingJab && !landscapeSecondarySource.isPlaying )	//#TODO: This
						{ landscapePlayingJab = false; rampLandscapeDown = false; rampLandscapeUp = true; }

					if ( portraitPlayingJab && !portraitSecondarySource.isPlaying )
						{ portraitPlayingJab = false; rampPortraitDown = false; rampPortraitUp = true; }

					if ( rampLandscapeDown )
					{
						landscapeMainSource.volume = Mathf.Lerp (1f, 0f, lsDownFloat / rampTime );
						lsDownFloat += Time.deltaTime;

						if ( lsDownFloat > rampTime ) { rampLandscapeDown = false; lsDownFloat = 0f; }
					}

					if ( rampLandscapeUp )
					{
						landscapeMainSource.volume = Mathf.Lerp (0f, 1f, lsUpFloat / rampTime );
						lsUpFloat += Time.deltaTime;

						if ( lsUpFloat > rampTime ) { rampLandscapeUp = false; lsUpFloat = 0f; }
					}

					if ( rampPortraitDown )
					{
						portraitMainSource.volume = Mathf.Lerp (1f, 0f, ptDownFloat / rampTime );
						ptDownFloat += Time.deltaTime;

						if ( ptDownFloat > rampTime ) { rampPortraitDown = false; ptDownFloat = 0f; }
					}

					if ( rampPortraitUp )
					{
						portraitMainSource.volume = Mathf.Lerp (0f, 1f, ptUpFloat / rampTime );
						ptUpFloat += Time.deltaTime;

						if ( ptUpFloat > rampTime ) { rampPortraitUp = false; ptUpFloat = 0f; }
					}

					if ( !singleton.gazeZones[1].active && timeSinceLastJab > minimumTimeBetweenJabs && Random.value > 0.99f )
					{
						Debug.Log ("make jab: landscape making jab");

						landscapeMainSource.volume = 0f;

						int currentJabIndex;
						do { currentJabIndex = Random.Range (0, landscapeJabs.Length - 1); }
							while ( currentJabIndex == lastLandscapeJabIndex );

						lastLandscapeJabIndex = currentJabIndex;

						landscapeSecondarySource.clip = landscapeJabs[currentJabIndex];
						landscapeSecondarySource.Play();

						landscapePlayingJab = true; rampLandscapeDown = true;
						timeSinceLastJab = 0f;
					}

					else if ( !singleton.gazeZones[0].active && timeSinceLastJab > minimumTimeBetweenJabs && Random.value > 0.99f )
					{
						Debug.Log ("make jab: portrait making jab");

						portraitMainSource.volume = 0f;

						int currentJabIndex;
						do { currentJabIndex = Random.Range (0, portraitJabs.Length - 1); }
							while ( currentJabIndex == lastPortraitJabIndex );

						lastPortraitJabIndex = currentJabIndex;

						portraitSecondarySource.clip = portraitJabs[currentJabIndex];
						portraitSecondarySource.Play();

						portraitPlayingJab = true; rampPortraitDown = true;
						timeSinceLastJab = 0f;
					}
				
					timeSinceLastJab += Time.deltaTime;
					lessonTimeElapsed += Time.deltaTime;
				}

				else 
				{	if ( !lessonStarted && singleton.currentTakeIndex == 1 )	{	lessonStarted = true;	} 	
					if ( lessonStarted && !lessonConcluded ) 
						{ portraitMainSource.volume = landscapeMainSource.volume = 1f; lessonConcluded = true; }
				}
			}
		}
	}
}
