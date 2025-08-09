using System;

namespace BcsSharp.Core.Formatters
{
    /// <summary>
    /// Represents the unit type () in BCS - serializes to zero bytes
    /// </summary>
    public readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Value = new();
        
        public bool Equals(Unit other) => true;
        public override bool Equals(object? obj) => obj is Unit;
        public override int GetHashCode() => 0;
        public override string ToString() => "()";
        
        public static bool operator ==(Unit left, Unit right) => true;
        public static bool operator !=(Unit left, Unit right) => false;
    }
    
    /// <summary>
    /// Formatter for the Unit type - serializes to zero bytes
    /// </summary>
    public sealed class UnitFormatter : IBcsFormatter<Unit>
    {
        public static readonly UnitFormatter Instance = new();
        public Type TargetType => typeof(Unit);
        
        public void Serialize(ref BcsWriter writer, Unit value)
        {
            // Unit type serializes to zero bytes
        }
        
        public Unit Deserialize(ref BcsReader reader)
        {
            // Unit type deserializes from zero bytes
            return Unit.Value;
        }
        
        public int? GetSerializedSize(Unit value) => 0;
    }
}