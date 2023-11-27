using Fusion;
using UnityEngine;

enum Buttons {
	Sprint, Jump, Crouch, Kill,
	Fire, Aim, Reload, 
	Weapon1, Weapon2,
	
}

public struct NetworkInputData : INetworkInput {
	public NetworkButtons buttons;
	public Vector2 movement;
	public Vector2 lookDelta;
	public Vector3 muzzlePos, muzzleDir;
}