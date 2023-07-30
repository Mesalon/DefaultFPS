using System.Collections.Generic;
using Fusion.Sockets;
using UnityEngine;
using Fusion;
using System;
using System.Linq;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks {
	public static Player GetPlayer(PlayerRef player) => inst.Runner.GetPlayerObject(player).GetComponent<Player>();
	public static Player LocalPlayer => GetPlayer(inst.Runner.LocalPlayer);
	public static Firearm GetWeapon(int index) => gunLibrary[index];
	public static Attachment GetAttachment(int index) {
		return attachmentLibrary[index];
	}

	public static List<Firearm> gunLibrary = new();
	public static List<Attachment> attachmentLibrary = new();
	
	[Networked] public int redTeamKills { get; set; }
	[Networked] public int blueTeamKills { get; set; }

	public static GameManager inst;
	public Camera mainCamera;
	public Camera activeCamera;
	public VolumeProfile menuProfile;
	private static List<Transform> spawns = new();
	[SerializeField] private PlayerSetup loadouts;
	[SerializeField] private TMP_InputField nameField;
	[SerializeField] private TMP_Text nameText;
	[SerializeField] private Transform spawnHolder;
	[SerializeField] private Volume postProcessing;
	[SerializeField] private NetworkPrefabRef playerPF;

	private void Awake() {
		if (!inst) { inst = this; }
		else if (inst != this) { Destroy(gameObject); }
		
		GameObject[] firearms = Resources.LoadAll("Firearms").OfType<GameObject>().ToArray();
		foreach (GameObject obj in firearms) { gunLibrary.Add(obj.GetComponent<Firearm>()); }
		GameObject[] attachments = Resources.LoadAll("Attachments").OfType<GameObject>().ToArray();
		for (int i = 0; i < attachments.Length; i++) {
			Attachment attachment = attachments[i].GetComponent<Attachment>();
			attachment.id = i;
			attachmentLibrary.Add(attachment);
		}
		
		foreach (Transform child in spawnHolder) { spawns.Add(child); }
		
		nameField.onSubmit.AddListener(input => {
			GetPlayer(Runner.LocalPlayer).RPC_SetName(input);
			nameField.text = "";
		});
		
		SwitchCamera(mainCamera, menuProfile);
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
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
		if (Runner.IsServer) {
			runner.Despawn(Runner.GetPlayerObject(player));
			Character c = Runner.GetPlayerObject(player).GetComponent<Player>().Character;
			if (c) { c.Health = 0; }
			Runner.SetPlayerObject(player, null);
		}
		print($"Destroyed player {player}");
	}

	public void SpawnCharacterHook() {
		WeaponConfiguration[] weapons = new WeaponConfiguration[2];
		for (int i = 0; i < weapons.Length; i++) {
			weapons[i].id = loadouts.lastWeapons[i].id;
			for (int i2 = 0; i2 < loadouts.lastWeapons[i].mountOptions.Count; i2++) {
				AttachmentMount mount = loadouts.lastWeapons[i].mountOptions[i2].mount;
				weapons[i].Attachments.Set(i2, mount.preview ? mount.preview.id : -1);
			}
		}
		LocalPlayer.RPC_SpawnCharacter(spawns[new NetworkRNG(Runner.Tick).RangeInclusive(0, spawns.Count - 1)].position, weapons);
	}

	public void SwitchCamera(Camera cam, VolumeProfile postFX = null) {
		if(activeCamera) { activeCamera.gameObject.SetActive(false); }
		cam.gameObject.SetActive(true);
		activeCamera = cam;
		if (postFX) { postProcessing.profile = postFX; }
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

public enum Team { Red, Blue }
