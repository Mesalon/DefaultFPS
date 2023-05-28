using Fusion;
using UnityEngine;

enum Buttons {
	Run, Jump, Fire, Aim, Reload,
}

public struct NetworkInputData : INetworkInput {
	public NetworkButtons buttons;
	public Vector2 movement;
	public Vector2 lookDelta;
	public Vector3 muzzlePos, muzzleDir;
}