using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GameManager : NetworkBehaviour
{
	public static GameManager inst { get; set; }
	private void Awake() { inst = this; }

	[SerializeField] Transform spawnHolder;
	private List<Transform> spawns = new();

}
