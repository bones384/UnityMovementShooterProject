using System;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    [Serializable]
    internal struct EulerAngles : IEquatable<EulerAngles>
    {
        public static EulerAngles Default => new() { RotationOrder = math.RotationOrder.ZXY };

        public float3 Value;
        [HideInInspector] public math.RotationOrder RotationOrder;

        internal void SetValue(quaternion value)
        {
            Value = math.degrees(value.ToEulerAngles(RotationOrder));
        }

        public static implicit operator quaternion(EulerAngles euler)
        {
            return math.normalize(quaternion.Euler(math.radians(euler.Value), euler.RotationOrder));
        }

        public static implicit operator EulerAngles(quaternion orientation)
        {
            var euler = new EulerAngles();
            euler.SetValue(orientation);
            return euler;
        }

        public bool Equals(EulerAngles other)
        {
            return Value.Equals(other.Value) && RotationOrder == other.RotationOrder;
        }

        public override bool Equals(object obj)
        {
            return obj is EulerAngles other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)math.hash(new float4(Value, (float)RotationOrder)));
        }
    }
}