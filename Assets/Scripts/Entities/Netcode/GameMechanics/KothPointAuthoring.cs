using Entities.Netcode.GameMechanics;
using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode.Authoring
{
    public class KothPointAuthoring : MonoBehaviour
    {
        public float Radius = 5f;
        public float Height = 4f;
        public float TimeToWin = 180f; // 3 minutes
        public float TimeToCapture = 10f;

        private class Baker : Baker<KothPointAuthoring>
        {
            public override void Bake(KothPointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new KothPointComponent
                {
                    Radius = authoring.Radius,
                    Height = authoring.Height,
                    TimeToWin = authoring.TimeToWin,
                    TimeToCapture = authoring.TimeToCapture,

                    TeamATimer = authoring.TimeToWin,
                    TeamBTimer = authoring.TimeToWin,
                    CurrentOwner = -1,
                    CapturingTeam = -1,
                    CaptureProgress = 0f
                });
            }
        }
    }
}