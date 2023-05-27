using UnityEngine;
using Fusion;

public class SessionManager : MonoBehaviour {
	private NetworkRunner runner;
	private GameManager game;

	private void Awake() {
		runner = GetComponent<NetworkRunner>();
		runner.ProvideInput = true;
	}

	private async void StartGame(GameMode mode) {
		await runner.StartGame(new StartGameArgs {
			GameMode = mode,
			SessionName = "Furry Fandom (LGBTQ+): Hangout & RP",
			Scene = 1,
			SceneManager = GetComponent<SceneLoader>()
		});
	}

	public void StartGameHook(int mode) {
		StartGame(mode switch {
			0 => GameMode.Host,
			1 => GameMode.Client,
			2 => GameMode.AutoHostOrClient,
			3 => GameMode.Single
		});
	}
}
