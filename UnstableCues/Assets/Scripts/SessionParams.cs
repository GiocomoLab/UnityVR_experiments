using UnityEngine;
using System.IO;

public class SessionParams : MonoBehaviour {

	public bool saveData = false;
	public string mouse;
	public string session;
	//private float speedVR = 1f;

	// for saving data
	public string localDirectory; // = 'C:\Users\giocomolab\Desktop\mice\' ; 
	public string serverDirectory; // = "Z:\giocomo\samjlevy\Sam_NPX\Unity_Comp_Data\mice";
	private string paramsFile;
	private string serverParamsFile;

	[HideInInspector] public string fullLocalStr;
	[HideInInspector] public string fullServerStr;

	public string trackName;

	void Start () 
	{
		fullLocalStr = localDirectory + "\\" + mouse + "\\" + session;
		fullServerStr = serverDirectory + "\\" + mouse + '\\' + session;

		paramsFile =  fullLocalStr + "_params.txt";
		serverParamsFile = fullServerStr + "_params.txt";

		string trackNameTmp = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		trackName = trackNameTmp.Substring (0, trackNameTmp.Length);

		/*
		if (saveData) 
		{
			var sw = new StreamWriter (paramsFile, true);
			sw.WriteLine ("TrackName\t" + trackName);
			sw.WriteLine("MouseName\t" + mouse);
			sw.WriteLine("session\t" + session);
			sw.Close();
		}
		*/
	}

	void OnApplicationQuit() 
	{
		if (saveData) 
		{
            try
            {
				File.Copy(paramsFile, serverParamsFile);
				Debug.Log("Copied the params file");
			}
            catch
            {

            }
		}
	}
}
