using Unity.Physics.Authoring;
using UnityEditor;
using UnityEditorInternal;

namespace Unity.Physics.Editor
{
    [CustomEditor(typeof(PhysicsCategoryNames))]
    [CanEditMultipleObjects]
    internal class PhysicsCategoryNamesEditor : BaseEditor
    {
#pragma warning disable 649
        [AutoPopulate(ElementFormatString = "Category {0}", Resizable = false, Reorderable = false)]
        private ReorderableList m_CategoryNames;
#pragma warning restore 649
    }
}