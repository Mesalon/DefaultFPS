using System;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class DebugSession : MonoBehaviour {
    [SerializeField] private bool singlePlayer;
    private NetworkRunner runner;

    private void Awake() {
        runner = GetComponent<NetworkRunner>();
        runner.ProvideInput = true;
        StartGame();
    }

    public async void StartGame() {
        await runner.StartGame(new StartGameArgs {
            GameMode = singlePlayer ? GameMode.Single : GameMode.AutoHostOrClient,
            SessionName = "Debug Room",
            Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
}