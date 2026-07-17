using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Physics.Authoring
{
    [CreateAssetMenu(menuName = "Unity Physics/Custom Physics Material Tag Names",
        fileName = "Custom Material Tag Names", order = 506)]
    public sealed class CustomPhysicsMaterialTagNames : ScriptableObject, ITagNames
    {
        [SerializeField] [FormerlySerializedAs("m_FlagNames")]
        private string[] m_TagNames = Enumerable.Range(0, 8).Select(i => string.Empty).ToArray();

        private CustomPhysicsMaterialTagNames()
        {
        }

        private void OnValidate()
        {
            if (m_TagNames.Length != 8)
                Array.Resize(ref m_TagNames, 8);
        }

        public IReadOnlyList<string> TagNames => m_TagNames;
    }
}