using Unity.Physics.Authoring;
using UnityEditor;
using UnityEditorInternal;

namespace Unity.Physics.Editor
{
    [CustomEditor(typeof(CustomPhysicsMaterialTagNames))]
    [CanEditMultipleObjects]
    internal class CustomPhysicsMaterialTagNamesEditor : BaseEditor
    {
#pragma warning disable 649
        [AutoPopulate(ElementFormatString = "Custom Physics Material Tag {0}", Resizable = false, Reorderable = false)]
        private ReorderableList m_TagNames;
#pragma warning restore 649
    }
}