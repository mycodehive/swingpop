using System;
using UnityEngine;

namespace SwingPop.Online
{
    [Serializable]
    public struct MatchId : IEquatable<MatchId>
    {
        [SerializeField] private string value;

        public MatchId(string value)
        {
            this.value = value ?? string.Empty;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(MatchId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MatchId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(MatchId left, MatchId right) => left.Equals(right);
        public static bool operator !=(MatchId left, MatchId right) => !left.Equals(right);
    }

    [Serializable]
    public struct MatchPlayerId : IEquatable<MatchPlayerId>
    {
        [SerializeField] private string value;

        public MatchPlayerId(string value)
        {
            this.value = value ?? string.Empty;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(MatchPlayerId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MatchPlayerId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(MatchPlayerId left, MatchPlayerId right) => left.Equals(right);
        public static bool operator !=(MatchPlayerId left, MatchPlayerId right) => !left.Equals(right);
    }

    [Serializable]
    public struct NetworkVector3 : IEquatable<NetworkVector3>
    {
        [SerializeField] private float x;
        [SerializeField] private float y;
        [SerializeField] private float z;

        public NetworkVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float X => x;
        public float Y => y;
        public float Z => z;
        public bool IsFinite => IsFiniteValue(x) && IsFiniteValue(y) && IsFiniteValue(z);
        public Vector3 ToUnity() => new(x, y, z);
        public static NetworkVector3 FromUnity(Vector3 value) => new(value.x, value.y, value.z);
        public bool Equals(NetworkVector3 other) => x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
        public override bool Equals(object obj) => obj is NetworkVector3 other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = x.GetHashCode();
                hash = (hash * 397) ^ y.GetHashCode();
                return (hash * 397) ^ z.GetHashCode();
            }
        }

        private static bool IsFiniteValue(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
