using Fusion;
using UnityEngine;

enum Buttons {
	Sprint, Jump, Fire, Aim, Reload,
}

public struct NetworkInputData : INetworkInput {
	public NetworkButtons buttons;
	public Vector2 movement;
	public Vector2 lookDelta;
}