using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectContrast : MonoBehaviour {

	private PlayerController2 playerScript;

	void Start () {
		// connect to playerController script
		GameObject player = GameObject.Find ("Player");
		gameObject.SetActive (true);
		playerScript = player.GetComponent<PlayerController2> ();
	}
}