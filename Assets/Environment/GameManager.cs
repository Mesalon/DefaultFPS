using System.Collections.Generic;
using Fusion.Sockets;
using UnityEngine;
using Fusion;
using System;
using TMPro;

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks {
	public Player LocalPlayer => Runner.GetPlayerObject(Runner.LocalPlayer).GetComponent<Player>();
	public static GameManager inst;
	public static List<Transform> spawns = new();
	[SerializeField] public Camera mainCamera;
	[SerializeField] private TMP_InputField nameField;
	[SerializeField] private TMP_Text nameText;
	[SerializeField] private NetworkPrefabRef playerPF;
	[SerializeField] private Transform spawnHolder;
	public Camera activeCamera;

	private void Awake() {
		if (!inst) { inst = this; }
		else if (inst != this) { Destroy(gameObject); }
		foreach (Transform child in spawnHolder) { spawns.Add(child); }
		SwitchCamera(mainCamera);
	}

	private void Start() {
		nameField.onSubmit.AddListener(input => {
			LocalPlayer.RPC_SetName(input);
			nameField.text = "";
		});
	}

	public override void Spawned() {
		Runner.AddCallbacks(this);
	}

	public override void Render() {
		if (Runner.TryGetPlayerObject(Runner.LocalPlayer, out NetworkObject obj)) {
			nameText.text = $"Name: {obj.GetComponent<Player>().Name}";
		}
	}

	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
		if (Runner.IsServer) {
			NetworkObject playerObject = runner.Spawn(playerPF, Vector3.zero, Quaternion.identity, player);
			Runner.SetPlayerObject(player, playerObject);
		}
		print($"Spawned player {player.PlayerId}");
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
		if (Runner.IsServer) {
			runner.Despawn(Runner.GetPlayerObject(player));
			Runner.GetPlayerObject(player).GetComponent<Player>().character.Kill();
			Runner.SetPlayerObject(player, null);
		}
		print($"Destroyed player {player}");
	}

	public void SpawnCharacterHook() {
		Runner.GetPlayerObject(Runner.LocalPlayer).GetComponent<Player>().RPC_SpawnCharacter(spawns[Runner.LocalPlayer % spawns.Count].position, Runner.LocalPlayer);
	}

	public void SwitchCamera(Camera cam) {
		if(activeCamera) { activeCamera.gameObject.SetActive(false); }
		cam.gameObject.SetActive(true);
		activeCamera = cam;
	}
	
	#region stubs
	public void OnInput(NetworkRunner runner, NetworkInput input) { }
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
	public void OnConnectedToServer(NetworkRunner runner) { }
	public void OnDisconnectedFromServer(NetworkRunner runner) { }
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
	#endregion
}
