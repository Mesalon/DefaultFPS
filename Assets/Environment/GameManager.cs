using System.Collections.Generic;
using Fusion.Sockets;
using UnityEngine;
using Fusion;
using System;
using TMPro;

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks {
	public static GameManager inst;
	public static Player GetPlayer(NetworkRunner runner, PlayerRef player) => runner.GetPlayerObject(player).GetComponent<Player>();
	public Camera mainCamera;
	public Camera activeCamera;
	public static List<Transform> spawns = new();

	[SerializeField] private TMP_InputField nameField;
	[SerializeField] private TMP_Text nameText;
	[SerializeField] private Transform spawnHolder;
	[SerializeField] private NetworkPrefabRef playerPF;
	public int redTeamCount;
	public int blueTeamCount;
	public int redTeamKills;
	public int blueTeamKills;
	public int killsToEndMatch;
	
	private void Awake() {
		if (!inst) { inst = this; }
		else if (inst != this) { Destroy(gameObject); }
		foreach (Transform child in spawnHolder) { spawns.Add(child); }
		
		nameField.onSubmit.AddListener(input => {
			GetPlayer(Runner, Runner.LocalPlayer).RPC_SetName(input);
			nameField.text = "";
		});
	}

	public override void Spawned() {
		Runner.AddCallbacks(this);
		SwitchCamera(mainCamera);
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
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
		if (Runner.IsServer) {
			runner.Despawn(Runner.GetPlayerObject(player));
			Character c = Runner.GetPlayerObject(player).GetComponent<Player>().character;
			if(c) { c.Kill(); }
			Runner.SetPlayerObject(player, null);
		}
		print($"Destroyed player {player}");
	}

	public void SpawnCharacterHook() {
		Runner.GetPlayerObject(Runner.LocalPlayer).GetComponent<Player>().RPC_SpawnCharacter(spawns[new NetworkRNG(Runner.Tick).RangeInclusive(0, spawns.Count)].position, Runner.LocalPlayer);
	}

	public void SwitchCamera(Camera cam) {
		if(activeCamera) { activeCamera.gameObject.SetActive(false); }
		cam.gameObject.SetActive(true);
		activeCamera = cam;
	}

    public enum Team {
		Red,
		Blue
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
