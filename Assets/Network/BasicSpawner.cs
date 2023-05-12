using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks {
	[SerializeField] NetworkPrefabRef playerPF;
	[SerializeField] Transform spawnHolder;
	private List<Transform> spawns = new();
	private Dictionary<PlayerRef, NetworkObject> characters = new();
	private NetworkRunner runner;

	private void Awake() {
		foreach (Transform child in spawnHolder) {
			spawns.Add(child);
		}
	}

	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
		if (runner.IsServer) {
			NetworkObject playerObject = runner.Spawn(playerPF, spawns[/*Random.Range(0, spawns.Count - 1)*/0].position, Quaternion.identity, player);
			characters.Add(player, playerObject);
		}
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
		if (characters.TryGetValue(player, out NetworkObject playerObject)) {
			runner.Despawn(playerObject);
			characters.Remove(player);
		}
	}

	public void OnInput(NetworkRunner runner, NetworkInput input) { }

	async void StartGame(GameMode mode) {
		runner = gameObject.AddComponent<NetworkRunner>();
		runner.ProvideInput = true;
		await runner.StartGame(new StartGameArgs {
			GameMode = mode,
			SessionName = "TestRoom",
			Scene = SceneManager.GetActiveScene().buildIndex,
			SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
		});
	}
	
	private void OnGUI() {
		if (!runner) {
			if (GUI.Button(new Rect(0,0,200,40), "Host")) { StartGame(GameMode.Host); } 
			if (GUI.Button(new Rect(0,40,200,40), "Join")) { StartGame(GameMode.Client); }
			if (GUI.Button(new Rect(0,80,200,40), "Singleplayer")) { StartGame(GameMode.Single); }
		}
	}

	#region stubs
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