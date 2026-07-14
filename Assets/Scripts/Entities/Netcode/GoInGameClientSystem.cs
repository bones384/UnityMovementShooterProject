using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Entities.Netcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    internal partial struct GoInGameSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithNone<NetworkStreamInGame>().WithEntityAccess())
            {
                buffer.AddComponent<NetworkStreamInGame>(entity);
                Debug.Log("Setting client as InGame");

                var rpcEntity = buffer.CreateEntity();
                buffer.AddComponent(rpcEntity, new GoInGameRequestRpc());
                buffer.AddComponent(rpcEntity, new SendRpcCommandRequest());
            }

            buffer.Playback(state.EntityManager);
        }
    }

    public struct GoInGameRequestRpc : IRpcCommand
    {
    }
}