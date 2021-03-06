﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class PowerUps : Poolable {

	public enum powerUp { unlimitedDrift,  waveEmit, shield, random}
	public powerUp type;

	public GameObject driftMegaMegaGauge;
	public float unlimitedDriftTime;
	public float waveEmitTime;
	public float delayBetweenWaves;
	public GameObject wavePrefab;
	public float shieldTime;
	Coroutine cor;
	
	bool taken;
	ShipBehaviour_V2 player;

	public void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Player" && !taken)
		{
			HidePowerUp();
			taken = true;
			player = other.transform.parent.gameObject.GetComponent<ShipBehaviour_V2>();
			Debug.Assert(player != null, "Player is null");
			switch (type)
			{
				case powerUp.unlimitedDrift:
					UnlimitedDrift();
					break;
				case powerUp.waveEmit:
					cor = StartCoroutine(WaveEmit());
					break;
				case powerUp.shield:
					cor = StartCoroutine(Shield());
					break;
			}
		}
	}

	private void HidePowerUp()
	{
        foreach(Transform child in transform) { child.gameObject.SetActive(false); }
		GetComponent<SphereCollider>().enabled = false;
	}

	private void UnlimitedDrift()
	{
		player.driftLossRatio = -1f;
		driftMegaMegaGauge = player.driftMegaMegaGauge;
		driftMegaMegaGauge.SetActive (true);
		Observable.Timer(TimeSpan.FromSeconds(unlimitedDriftTime))
			.Subscribe(_ =>
			{
				player.driftLossRatio = 1f;
				driftMegaMegaGauge.SetActive(false);
				parent.Recycle(this);
			}).AddTo(this);
	}


	IEnumerator WaveEmit()
	{
		float t = (waveEmitTime / delayBetweenWaves);
		GameObject go = Instantiate(wavePrefab, player.transform.position, Quaternion.identity);
		go.GetComponent<PlayerWave>().baseCol = player.powerUpWaveEmitColor;
		go.GetComponent<PlayerWave>().playerID = player.playerID;
		for (int i=0; i<t; i++)
		{
			yield return new WaitForSeconds(delayBetweenWaves);
			if (player.death)
				yield break;
			GameObject goo = Instantiate(wavePrefab, player.transform.position, Quaternion.identity);
			goo.GetComponent<PlayerWave>().baseCol = player.powerUpWaveEmitColor;
			goo.GetComponent<PlayerWave>().playerID = player.playerID;
		}
		parent.Recycle(this);
	}

	IEnumerator Shield()
	{
		player.shield = true;
		player.ship.GetComponent<MeshRenderer> ().material = player.shieldMat;
		yield return new WaitForSeconds(shieldTime);
		player.shield = false;
		player.ship.GetComponent<MeshRenderer> ().material = player.baseMat;
		parent.Recycle(this);
	}

	public override Poolable Init(Pool parent)
	{
		this.parent = parent;
		gameObject.SetActive(false);
		taken = false;
		return this;
	}

	public override void Spawn(object args)
	{
		Debug.Assert (args is Vector3, "Args is not vector3");
		transform.position = (Vector3)args;
		gameObject.SetActive(true);
		if (type == powerUp.random)
		{
			int random = UnityEngine.Random.Range(0, Enum.GetValues(typeof(powerUp)).Length - 1);
			type = (powerUp)random;
		}
	}

	public override void Recycle()
	{
		gameObject.SetActive(false);
		GetComponent<SphereCollider>().enabled = true;
		type = powerUp.random;
		taken = false;
        foreach (Transform child in transform) { child.gameObject.SetActive(true); }
    }
}
