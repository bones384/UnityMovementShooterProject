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
        // Maps the ECS Entity to the Unity GameObject
        private Dictionary<Entity, GameObject> _trails = new Dictionary<Entity, GameObject>();
        private List<Entity> _deadProjectiles = new List<Entity>();

        protected override void OnUpdate()
        {
            if (ProjectileVisualisationManager.Instance == null || ProjectileVisualisationManager.Instance.rocketTrailPrefab == null) 
                return;

            // 1. Sync active projectiles
            foreach (var (projectile, transform, entity) in SystemAPI.Query<RefRO<ProjectileComponent>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                // If this is a brand new projectile, spawn a trail for it!
                if (!_trails.TryGetValue(entity, out GameObject trailObj))
                {
                    trailObj = Object.Instantiate(ProjectileVisualisationManager.Instance.rocketTrailPrefab);
                    _trails[entity] = trailObj;
                }

                // If the client predicted a hit and teleported it to the abyss...
                if (transform.ValueRO.Position.y < -9000f)
                {
                    DetachAndFade(entity, trailObj);
                }
                else
                {
                    // Snap the GameObject to the exact ECS coordinates
                    trailObj.transform.position = transform.ValueRO.Position;
                    trailObj.transform.rotation = transform.ValueRO.Rotation;
                }
            }

            // 2. Cleanup server-destroyed projectiles
            // (If the server destroyed the entity before the client predicted a hit)
            _deadProjectiles.Clear();
            foreach (var kvp in _trails)
            {
                if (!SystemAPI.Exists(kvp.Key))
                {
                    DetachAndFade(kvp.Key, kvp.Value);
                }
            }

            // 3. Remove dead entries from our tracking dictionary
            foreach (var dead in _deadProjectiles)
            {
                _trails.Remove(dead);
            }
        }

        private void DetachAndFade(Entity entity, GameObject trailObj)
        {
            if (trailObj != null)
            {
                // Turn off emission so the trail stops growing
                var particles = trailObj.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particles)
                {
                    var emission = ps.emission;
                    emission.enabled = false;
                }
                
                // Destroy the GameObject after 2 seconds to let the existing smoke fade out
                Object.Destroy(trailObj, 2f);
            }
            
            // Mark for removal from the dictionary
            _deadProjectiles.Add(entity);
        }
    }
}