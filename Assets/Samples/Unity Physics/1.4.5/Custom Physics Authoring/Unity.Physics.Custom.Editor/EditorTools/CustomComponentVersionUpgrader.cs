using System;
using Unity.Physics.Authoring;
using UnityEditor;

#if UNITY_EDITOR

namespace Unity.Physics.Editor
{
    internal static class CustomComponentVersionUpgrader
    {
        [MenuItem("Tools/Unity Physics/Upgrade Physics Shape Versions")]
        private static void UpgradeAssetsWithPhysicsShape()
        {
            var componentNeedsUpgradeFunction =
                new Func<PhysicsShapeAuthoring, bool>(component => component.NeedsVersionUpgrade);
            ComponentVersionUpgrader.UpgradeAssets("Physics Shape", componentNeedsUpgradeFunction);
        }
    }
}

#endif