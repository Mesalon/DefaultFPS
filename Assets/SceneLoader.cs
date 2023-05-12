using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Fusion;

public class SceneLoader : NetworkSceneManagerBase {
    [SerializeField] private GameObject loadScreen;
    
    protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished) {
        Debug.Log($"Switching scene from {(int)prevScene} to {(int)newScene}");
        loadScreen.SetActive(true);
        yield return SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Single);
        Scene loadedScene = SceneManager.GetSceneByBuildIndex(newScene);
        List<NetworkObject> sceneObjects = FindNetworkObjects(loadedScene, disable: false);
        yield return null;
        finished(sceneObjects);
        loadScreen.SetActive(false);
        Debug.Log($"Finished scene switch to scene: {(int)newScene}");
    }
}
