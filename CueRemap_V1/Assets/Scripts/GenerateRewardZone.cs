using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateRewardZone : MonoBehaviour {

	private int zoneCenter;
	private Color color;
	private int rewardPosition_local = 0;
	private int rewardTrial_local = 0;
	private int numTraversals_local = 0;

	private PlayerController3 playerScript;

	void Start () {
		// find player
		GameObject player = GameObject.Find ("Player");
		playerScript = player.GetComponent<PlayerController3> ();
	}

	void Update () {
		if (numTraversals_local != playerScript.numTraversals | playerScript.rewardPosition != rewardPosition_local | playerScript.rewardTrial != rewardTrial_local) {
			rewardPosition_local = (int) playerScript.rewardPosition;
			rewardTrial_local = playerScript.rewardTrial;
			numTraversals_local = playerScript.numTraversals;

			StartCoroutine (UpdateRewardZone ());
		}
	}

	IEnumerator UpdateRewardZone() {
		// get zone location
		zoneCenter = rewardPosition_local;

		if (numTraversals_local == rewardTrial_local) {
			// position zone
			transform.position = new Vector3 (0, 1, zoneCenter);
		}
		else {
			transform.position = new Vector3 (0, 1, zoneCenter + 800);
		}
		yield return null;
	}

}