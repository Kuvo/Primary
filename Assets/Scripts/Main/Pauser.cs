﻿using UnityEngine;
using System;
using System.Collections.Generic;

public class Pauser : MonoBehaviour
{
	/// <summary>
	/// ポーズ中かどうか
	/// </summary>
	public static bool isPause { get; private set; }

	private static List<Pauser> targets = new List<Pauser>();   // ポーズ対象のスクリプト

	// ポーズ対象のコンポーネント
	private List<Behaviour> pauseBehaviours { get; set; }
	private List<Rigidbody> rigidbodies { get; set; }
	private List<Vector3> rigidbodyVelocities { get; set; }
	private List<Vector3> rigidbodyAngularVelocities { get; set; }
	private List<Rigidbody2D> rigidbody2Ds { get; set; }
	private List<Vector2> rigidbody2DVelocities { get; set; }
	private List<float> rigidbody2DAngularVelocities { get; set; }
	private List<ParticleSystem> particleSystems { get; set; }

	/// <summary>
	/// OnLevelWasLoadedよりも呼び出しが遅い性質を利用
	/// シーン切り替え時の初期化後に実行
	/// </summary>
	void Start()
	{
		targets.Add(this);
		Initialize();
	}

	/// <summary>
	/// 初期化
	/// </summary>
	void Initialize()
	{
		pauseBehaviours = new List<Behaviour>();
		rigidbodies = new List<Rigidbody>();
		rigidbodyVelocities = new List<Vector3>();
		rigidbodyAngularVelocities = new List<Vector3>();
		rigidbody2Ds = new List<Rigidbody2D>();
		rigidbody2DVelocities = new List<Vector2>();
		rigidbody2DAngularVelocities = new List<float>();
		particleSystems = new List<ParticleSystem>();

		isPause = false;
	}

	/// <summary>
	/// 破棄されるとき
	/// </summary>
	void OnDestory()
	{
		// ポーズ対象から除外する
		targets.Remove(this);
	}

	/// <summary>
	/// ポーズされたとき
	/// </summary>
	void OnPause()
	{
		if (pauseBehaviours.Count > 0)
		{
			return;
		}
		// 有効なBehaviourをポーズさせる
		pauseBehaviours.AddRange(Array.FindAll(GetComponentsInChildren<Behaviour>(), (obj) => { return obj.enabled; }));
		foreach (Behaviour behaviour in pauseBehaviours)
		{
			if((behaviour is Pauser) || (behaviour is SetMiniMapMaterial))
			{
				continue;
			}

			if (!behaviour.GetComponent<Camera>() && !behaviour.GetComponent<Light>())
			{
				behaviour.enabled = false;
			}
		}

		// 有効なRigidbodyをポーズさせ、ポーズ直前の速度と角速度を記憶する
		rigidbodies.AddRange(Array.FindAll(GetComponentsInChildren<Rigidbody>(), (obj) => { return !obj.IsSleeping(); }));
		for (int i = 0; i < rigidbodies.Count; ++i)
		{
			rigidbodyVelocities.Add(rigidbodies[i].velocity);
			rigidbodyAngularVelocities.Add(rigidbodies[i].angularVelocity);
			rigidbodies[i].Sleep();
		}

		// 上記のRigidbody2D版
		rigidbody2Ds.AddRange(Array.FindAll(GetComponentsInChildren<Rigidbody2D>(), (obj) => { return !obj.IsSleeping(); }));
		for (int i = 0; i < rigidbody2Ds.Count; ++i)
		{
			rigidbody2DVelocities[i] = rigidbody2Ds[i].velocity;
			rigidbody2DAngularVelocities[i] = rigidbody2Ds[i].angularVelocity;
			rigidbody2Ds[i].Sleep();
		}

		// 有効なParticleSystemをポーズさせる
		particleSystems.AddRange(Array.FindAll(GetComponentsInChildren<ParticleSystem>(), (obj) => { return !obj.isPaused && !obj.isStopped; }));
		foreach (ParticleSystem particleSystem in particleSystems)
		{
			particleSystem.Pause();
		}

		isPause = true;
	}

	/// <summary>
	/// ポーズ解除されたとき
	/// </summary>
	void OnResume()
	{
		if (pauseBehaviours.Count <= 0)
		{
			return;
		}

		// ポーズ前の状態に各コンポーネントの有効状態を復元する
		foreach (Behaviour behaviour in pauseBehaviours)
		{
			behaviour.enabled = true;
		}

		for (int i = 0; i < rigidbodies.Count; ++i)
		{
			rigidbodies[i].WakeUp();
			rigidbodies[i].velocity = rigidbodyVelocities[i];
			rigidbodies[i].angularVelocity = rigidbodyAngularVelocities[i];
		}

		for (int i = 0; i < rigidbody2Ds.Count; ++i)
		{
			rigidbody2Ds[i].WakeUp();
			rigidbody2Ds[i].velocity = rigidbody2DVelocities[i];
			rigidbody2Ds[i].angularVelocity = rigidbody2DAngularVelocities[i];
		}

		foreach (ParticleSystem particleSystem in particleSystems)
		{
			particleSystem.Play();
		}

		Initialize();
	}

	/// <summary>
	/// ポーズする
	/// </summary>
	public static void Pause()
	{
		foreach (Pauser target in targets)
		{
			target.OnPause();
		}
	}

	/// <summary>
	/// ポーズを解除する
	/// </summary>
	public static void Resume()
	{
		foreach (Pauser target in targets)
		{
			target.OnResume();
		}
	}

	/// <summary>
	/// シーン切り替え時の初期化
	/// </summary>
	public static void SceneChangeInitialize()
	{
		targets = new List<Pauser>();
	}
}