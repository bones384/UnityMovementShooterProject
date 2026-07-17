using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;

public class GameplayBootstrapper : MonoBehaviour
{
    [Tooltip("Drag your ECS SubScene here from the Project window")]
    public EntitySceneReference gameplaySubScene;

    private void Start()
    {
        var loadParameters = new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnStreamIn };

        // 1. Load the level into the Client World (Everyone gets this)
        if (ClientServerBootstrap.ClientWorld != null)
            SceneSystem.LoadSceneAsync(ClientServerBootstrap.ClientWorld.Unmanaged, gameplaySubScene, loadParameters);

        // 2. Load the level into the Server World (Only the Host has this)
        if (MatchmakingState.IsHost && ClientServerBootstrap.ServerWorld != null)
            SceneSystem.LoadSceneAsync(ClientServerBootstrap.ServerWorld.Unmanaged, gameplaySubScene, loadParameters);
    }

    private void OnDestroy()
    {
        // Clean up the worlds if we leave the Gameplay scene to go back to the Main Menu
        if (ClientServerBootstrap.ClientWorld != null) ClientServerBootstrap.ClientWorld.Dispose();
        if (ClientServerBootstrap.ServerWorld != null) ClientServerBootstrap.ServerWorld.Dispose();
    }
}