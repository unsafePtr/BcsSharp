using System;

namespace BcsSharp.Core.Types
{
    /// <summary>
    /// BCS type for unit/empty values
    /// </summary>
    public class UnitType : BcsType<Unit>
    {
        public UnitType() : base("unit") { }

        public override Unit Read(BcsReader reader) => Unit.Value;

        public override void Write(Unit value, BcsWriter writer) 
        {
            // Unit type serializes to nothing (0 bytes)
        }

        public override int? SerializedSize(Unit value) => 0;
    }

    /// <summary>
    /// Represents the unit type (empty value)
    /// </summary>
    public readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Value = new();

        public bool Equals(Unit other) => true;

        public override bool Equals(object? obj) => obj is Unit;

        public override int GetHashCode() => 0;

        public static bool operator ==(Unit left, Unit right) => true;

        public static bool operator !=(Unit left, Unit right) => false;

        public override string ToString() => "()";
    }
}