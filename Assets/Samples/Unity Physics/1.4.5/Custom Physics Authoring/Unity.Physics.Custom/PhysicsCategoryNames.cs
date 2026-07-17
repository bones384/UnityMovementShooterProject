using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    [CreateAssetMenu(menuName = "Unity Physics/Physics Category Names", fileName = "Physics Category Names",
        order = 507)]
    public sealed class PhysicsCategoryNames : ScriptableObject, ITagNames
    {
        [SerializeField] private string[] m_CategoryNames = Enumerable.Range(0, 32).Select(i => string.Empty).ToArray();

        private PhysicsCategoryNames()
        {
        }

        public IReadOnlyList<string> CategoryNames => m_CategoryNames;

        private void OnValidate()
        {
            if (m_CategoryNames.Length != 32)
                Array.Resize(ref m_CategoryNames, 32);
        }

        IReadOnlyList<string> ITagNames.TagNames => CategoryNames;
    }
}