using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Entities.Netcode
{ 
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    partial struct NewISystemScript : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<NetworkId>();
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buffer = new EntityCommandBuffer(Allocator.Temp);

            EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            
            foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity entity) in SystemAPI
                         .Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRpc>().WithEntityAccess())
            {
                buffer.AddComponent<NetworkStreamInGame> (
                    receiveRpcCommandRequest.ValueRO.SourceConnection
                );
                Debug.Log("Client connected to server!");

                var playerEntity = buffer.Instantiate(entitiesReferences.playerPrefabEntity);
                buffer.SetComponent(playerEntity,LocalTransform.FromPosition(new float3
                (
                    UnityEngine.Random.Range(-10,+10),
                    0,
                    0
                )));
                NetworkId networkId =
                    SystemAPI.GetComponent<NetworkId>(receiveRpcCommandRequest.ValueRO.SourceConnection);
                buffer.AddComponent(playerEntity,new GhostOwner{
                    NetworkId = networkId.Value
                    });
                
                buffer.AppendToBuffer(receiveRpcCommandRequest.ValueRO.SourceConnection, new LinkedEntityGroup
                {
                     Value =  playerEntity
                });
                
                buffer.DestroyEntity(entity);
  
            }
            buffer.Playback(state.EntityManager);
        }

    }
}