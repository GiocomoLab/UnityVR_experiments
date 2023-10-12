using UnityEngine;
using System.Collections;
using System.IO;

public class SessionParams : MonoBehaviour {

	public bool saveData = false;
	public string mouse;
	public string session;
	private float speedVR = 1f;

	// for saving data
	public string localDirectory = "/Users/malcolmc/Desktop";
	public string serverDirectory = "/Volumes/data/Users/MCampbell";
	private string paramsFile;
	private string serverParamsFile;

	// teleport params
	public float trackStart = 0f;
	public float trackEnd = 400f;

	// reward params
	public bool lickForReward = false;
	public float pReward = 0.0025f;

	// session params
	public int numTrialsTotal = 100;
	public int numTrialsPerBlock = 10;
	public int numReminderTrials = 1;
	public int numReminderTrialsBegin = 30;
	public int numReminderTrialsEnd = 20;

	// gain manip params
	public bool manipSession = false;
	public int numGainTrials = 0;
	public float endGain = 0.5f;
	public float[] gains = new float[] {1};
	
	void Start () 
	{

		paramsFile = localDirectory + "\\" + mouse + "\\" + session + "_params.txt";
		serverParamsFile = serverDirectory + "\\" + mouse + "\\VR\\" + session + "_params.txt";

		string trackName = UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name;
		trackName = trackName.Substring (0, trackName.Length);

		if (saveData) 
		{
			var sw = new StreamWriter (paramsFile, true);
			sw.WriteLine ("TrackName\t" + trackName);
			sw.WriteLine ("manipSession\t" + manipSession);
			sw.WriteLine ("TrackStart\t" + trackStart);
			sw.WriteLine ("TrackEnd\t" + trackEnd);
			sw.WriteLine ("LickForReward\t" + lickForReward);
			sw.WriteLine ("NumReminderTrialsEnd\t" + numReminderTrialsEnd);
			sw.WriteLine ("NumReminderTrialsBegin\t" + numReminderTrialsBegin);
			sw.Close ();
		}
	}

	void OnApplicationQuit() 
	{
		if (saveData) 
		{
			File.Copy (paramsFile, serverParamsFile);
		}
	}
}
