using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;

public class GameplayBootstrapper : MonoBehaviour
{
    public EntitySceneReference gameplaySubScene;

    private void Start()
    {
        var loadParameters = new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnStreamIn };
        
        if (ClientServerBootstrap.ClientWorld != null)
            SceneSystem.LoadSceneAsync(ClientServerBootstrap.ClientWorld.Unmanaged, gameplaySubScene, loadParameters);
        
        if (MatchmakingState.IsHost && ClientServerBootstrap.ServerWorld != null)
            SceneSystem.LoadSceneAsync(ClientServerBootstrap.ServerWorld.Unmanaged, gameplaySubScene, loadParameters);
    }

    private void OnDestroy()
    {
        if (ClientServerBootstrap.ClientWorld != null) ClientServerBootstrap.ClientWorld.Dispose();
        if (ClientServerBootstrap.ServerWorld != null) ClientServerBootstrap.ServerWorld.Dispose();
    }
}