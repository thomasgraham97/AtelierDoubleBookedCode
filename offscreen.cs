/* OFFSCREEN.CS

This script handles:

i. gaze tracking values
ii. scene storage and selection
iii. actor animation and management

It interfaces between:

i. scene properties, defined through the Unity Inspector window
ii. scene objects, built and manipulated at runtime to the spec of the scene properties*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public struct performance //Performances are a selection from a pool of takes...
{
	public string name;

	public bool skip;
	public take[] takes;
	public int selectedTakeIndex;
}

[System.Serializable] public struct take //...takes map video to actors...
{
	public string name;

	[HideInInspector]public bool skip;
	public actor[] actors;
	public zone[] gazeZones;
	public UnityEngine.Video.VideoClip video;
}

[System.Serializable] public struct actor //...actors are keyframe-animated spatialized audio...
{
	public string name;

	public AudioClip mainAudioClip;
	public AudioClip[] secondaryAudioClips;
	[HideInInspector] public int prevSecAudioClipIndex;
	[HideInInspector] public AudioSource mainAudioSource, secondaryAudioSource; //Multiple audio sources to simplify trigger-based audio events
	[HideInInspector] public ONSPAudioSource onspSource, secondaryONSPSource;

	public Vector3 position;

	public keyframe[] animation;
	[HideInInspector] public Vector3 keyframeStartPosition;
	[HideInInspector] public keyframe currentKeyframe;
	[HideInInspector] public int currentKeyframeIndex;
	[HideInInspector] public float frameTimeElapsed, frameDuration;
}

[System.Serializable] public struct keyframe //...and keyframes are points in a sequence of time and place
{
	public float time;
	public Vector3 position;
}

[System.Serializable] public struct zone //Zones define angle of vision, and store gaze time within those areas
{
	public string name;

	public float min, max; //Confined to the y-axis -- if I knew quaternion math better, this could be more flexible...
	public bool active; //Is this zone being looked at?
	public float time;
}

public class offscreen : MonoBehaviour 
{	
	/*A singleton property--lets us access 
	this instance's properties through its class, 
	making it global*/

	public static offscreen singleton;
	public bool debug;	//Show debug messages?

	/*
	Take management variables
	*/

	public performance[] sequence; //A series of selected takes
	[HideInInspector]public take[] takes; //The actual take objects we'll be handling

	[HideInInspector]public take currentTake; //The current take object
	[HideInInspector]public int currentTakeIndex;

	/*
	Scene variables
	*/

	GameObject[] actors;
	GameObject actorsDummy; //Solely exists to keep the Hierachy panel organized in the Unity Editor

	/*
	Playback variables
	*/

	public UnityEngine.Video.VideoPlayer player;
	public UnityEngine.Audio.AudioMixerGroup mixer; //Drag and drop reference to Oculus Spatializer mixer via Inspector
	[HideInInspector]public float runningTime;
	[HideInInspector]public bool started, finished;

	/*
	Gaze-tracking variables
	*/

	public Transform viewer;
	[HideInInspector]public zone[] gazeZones;

	void Start()
	{
		if ( offscreen.singleton == null ) { offscreen.singleton = this; } //Define this instance as the singleton variable
			else { GameObject.Destroy (this); }

		/*
		Scene property setup
		*/

		takes = new take[sequence.Length]; //Set up the takes 

		for ( int i = 0; i < sequence.Length; i++ )
		{
			int fixedIndex = //Make sure the take index actually fits the take pool's array boundaries
				Mathf.Max (0, Mathf.Min ( sequence[i].selectedTakeIndex, sequence[i].takes.Length - 1 ) );

			takes[i] = sequence[i].takes[fixedIndex]; //Set each take in the take array as the one chosen from the take pool ('sequence' variable)
			takes[i].skip = sequence[i].skip; //Transfer properties
			takes[i].name = sequence[i].name;

			if (debug ) { Debug.Log ("take manager: " + takes[i].name + " skip value " + takes[fixedIndex].skip ); }
		}

		while ( takes[currentTakeIndex].skip ) { currentTakeIndex++; }	//Set the current take as the first in the take array that doesn't have a skip flag
		currentTake = takes[currentTakeIndex];

		/*
		Scene object setup
		*/

		actorsDummy = new GameObject(); //Just to keep the Inspector hierachy organized
		actorsDummy.name = "actors";

		player.playOnAwake = false;
		player.loopPointReached += advanceTake; //Move on to the next take when the video clip ends

		gazeZones = currentTake.gazeZones;
		player.clip = currentTake.video; 
		player.Prepare();

		//Animation setup

		for ( int i = 0; i < currentTake.actors.Length; i++ )
		{
			if ( currentTake.actors[i].animation.Length > 0 )
			{
				currentTake.actors[i].keyframeStartPosition = currentTake.actors[i].position;
				currentTake.actors[i].currentKeyframe = currentTake.actors[i].animation[0]; //Load the first keyframe
				currentTake.actors[i].frameDuration = currentTake.actors[i].currentKeyframe.time; //Since we're at 0s, duration = time
			}
		}
	}

	void Update()
	{
		/*
		Placeholder start
		*/

		if (player.isPrepared && !started) { begin(); } //Don't start anything until the player is ready to go

		if ( started )
		{
			/*
			Gaze-tracking loop
			*/

			for ( int i = 0; i < gazeZones.Length; i++ )
			{	if ( viewer.eulerAngles.y > gazeZones[i].min && viewer.eulerAngles.y < gazeZones[i].max )
				{
					gazeZones[i].active = true;
					gazeZones[i].time += Time.deltaTime;

					//if (debug) 
					//{ Debug.Log ( gazeZones[i].name + " gaze time | " + gazeZones[i].time ); }
				}
				else if (gazeZones[i].active) { gazeZones[i].active = false; }
			}

			/*
			Animation loop
			*/

			for ( int i = 0; i < currentTake.actors.Length; i++ )
			{
				actor a = currentTake.actors[i];

				if ( a.currentKeyframeIndex < a.animation.Length )
				{
					actors[i].transform.position = Vector3.Lerp
					(
						a.keyframeStartPosition,
						a.currentKeyframe.position,
						a.frameTimeElapsed / a.frameDuration
					);

					if ( a.frameTimeElapsed > a.frameDuration )
					{ 
						if (debug) { Debug.Log ("animation manager: advancing keyframe for " + a.name + ", index " + a.currentKeyframeIndex + "/" + a.animation.Length); }

						if ( a.currentKeyframeIndex < a.animation.Length - 1 )
						{
							a.keyframeStartPosition = actors[i].transform.position; //Set keyframe start to actor's current position

							a.currentKeyframeIndex++; //Move to the next keyframe
							a.currentKeyframe = a.animation[a.currentKeyframeIndex];

							a.frameTimeElapsed = 0f; //Reset the time elapsed
							a.frameDuration = a.currentKeyframe.time - runningTime;
						}

						if (debug)	//Draw a line following the animation path
						{ 
							Debug.DrawLine 
							(
								a.keyframeStartPosition,
								a.currentKeyframe.position,
								Random.ColorHSV(),
								a.frameDuration
							);
						}
					}
				}

				a.frameTimeElapsed += Time.deltaTime;

				if (debug)
				{ 
					Debug.Log 
					( "animation manager: " + a.name + " frame " + i + 
					" lerp ratio is " + a.frameTimeElapsed / a.frameDuration + "\n" +
					" time elapsed is " + a.frameTimeElapsed + "\n" +
					" frame duration is " + a.frameDuration ); 
				}

				currentTake.actors[i] = a;
			}

			runningTime += Time.deltaTime;
		}
	}

	public void begin() //Kicks off playback
	{
		if (!started)
		{
			spawnActors();
			player.Play();

			started = true;
		}
	}

	void advanceTake ( UnityEngine.Video.VideoPlayer vp )
	{
		if (debug) {Debug.Log ("take manager: end of " + currentTake.name);}

		if ( currentTakeIndex < takes.Length - 1 ) //If we're at the second-to-last index of the take array
		{
			do { currentTakeIndex++; } while ( takes[currentTakeIndex].skip );

			//First, some clean-up
			currentTake.actors = null; //Wipe references to actor objects in the scene

			if ( actors != null )
			{	for ( int i = 0; i < actors.Length; i++ ) //Then delete the actor objects
					{ Destroy(actors[i]); }
			}

			//And now to business

			currentTake = takes[currentTakeIndex];

			if (debug) { Debug.Log ("take manager: starting " + currentTake.name); }

			player.clip = currentTake.video; //Advance the player's clip (plays automatically)
			spawnActors(); //Then, re-spawn new actors based on the take's spec
			gazeZones = currentTake.gazeZones;

			for ( int i = 0; i < currentTake.actors.Length; i++ )
			{
				if ( currentTake.actors[i].animation.Length > 0 )
				{
					currentTake.actors[i].currentKeyframe = currentTake.actors[i].animation[0];
					currentTake.actors[i].frameDuration = currentTake.actors[i].currentKeyframe.time;
				}
			}

			player.Play();
		}

		else 
		{
			if (debug) {Debug.Log ("take manager: finished playback");}
			finished = true;
		}
	}

	void spawnActors()
	{
		actors = new GameObject[currentTake.actors.Length];

		for ( int i = 0; i < currentTake.actors.Length; i++ ) //Load actors as procedurally-generated GameObjects
		{
			GameObject a = actors[i]; //Shorthands: 'a' is the actor GameObject, 'b' is the actor properties
			actor b = currentTake.actors[i]; 

			a = new GameObject(); //Create the actual GameObject
			a.name = b.name;
			a.transform.parent = actorsDummy.transform;
			a.transform.position = b.position;

			b.mainAudioSource = a.AddComponent<AudioSource>(); //Create and associate its components
			b.mainAudioSource.outputAudioMixerGroup = mixer;
			b.mainAudioSource.spatialize = true;

			b.onspSource = a.AddComponent<ONSPAudioSource>();
			b.onspSource.EnableRfl = true;
			b.onspSource.UseInvSqr = true; //Use Oculus attenuation
			b.onspSource.Near = 2.5f;
			b.onspSource.Far = 15f;

			//Secondary audio

			GameObject secondaryDummy = new GameObject();
			ONSPAudioSource secondaryONSP;

			secondaryDummy.name = "secondary";
			secondaryDummy.transform.parent = a.transform;
			secondaryDummy.transform.localPosition = Vector3.zero;

			b.secondaryAudioSource = secondaryDummy.AddComponent<AudioSource>();
			b.secondaryAudioSource.outputAudioMixerGroup = mixer;
			b.secondaryAudioSource.spatialize = true;

			secondaryONSP = secondaryDummy.AddComponent<ONSPAudioSource>();
			secondaryONSP.EnableRfl = true;
			secondaryONSP.UseInvSqr = true; //Use Oculus attenuation
			secondaryONSP.Near = 1.5f;
			secondaryONSP.Far = 10f;

			b.mainAudioSource.clip = b.mainAudioClip; //Set key values for those components
			b.mainAudioSource.Play();

			actors[i] = a; currentTake.actors[i] = b;
		}
	}
}
