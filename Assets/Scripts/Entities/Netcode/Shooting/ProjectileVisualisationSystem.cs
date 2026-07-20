using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Entities.Netcode.Shooting
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ProjectileVisualisationSystem : SystemBase
    {
        private readonly List<Entity> _deadProjectiles = new();
        
        private readonly Dictionary<Entity, GameObject> _trails = new();

        protected override void OnUpdate()
        {
            if (ProjectileVisualisationManager.Instance == null ||
                ProjectileVisualisationManager.Instance.rocketTrailPrefab == null)
                return;
            
            foreach (var (projectile, transform, entity) in SystemAPI
                         .Query<RefRO<ProjectileComponent>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (!_trails.TryGetValue(entity, out var trailObj))
                {
                    trailObj = Object.Instantiate(ProjectileVisualisationManager.Instance.rocketTrailPrefab);
                    _trails[entity] = trailObj;
                }
                
                if (transform.ValueRO.Position.y < -9000f)
                {
                    DetachAndFade(entity, trailObj);
                }
                else
                {
                    trailObj.transform.position = transform.ValueRO.Position;
                    trailObj.transform.rotation = transform.ValueRO.Rotation;
                }
            }
            
            _deadProjectiles.Clear();
            foreach (var kvp in _trails)
                if (!SystemAPI.Exists(kvp.Key))
                    DetachAndFade(kvp.Key, kvp.Value);
            
            foreach (var dead in _deadProjectiles) _trails.Remove(dead);
        }

        private void DetachAndFade(Entity entity, GameObject trailObj)
        {
            if (trailObj != null)
            {
                var particles = trailObj.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particles)
                {
                    var emission = ps.emission;
                    emission.enabled = false;
                }
                
                Object.Destroy(trailObj, 2f);
            }
            
            _deadProjectiles.Add(entity);
        }
    }
}