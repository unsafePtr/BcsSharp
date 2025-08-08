using System.Diagnostics;
using BcsSharp.Core;
using BcsSharp.Core.Types;

namespace Sui.Types;

/// <summary>
/// Represents a complete object reference in Sui
/// Contains object ID, version, and digest for uniquely identifying an object state
/// </summary>
[DebuggerDisplay("{ToString()}")]
public record ObjectRef
{
    /// <summary>
    /// The object's unique identifier
    /// </summary>
    public ObjectId ObjectId { get; init; }

    /// <summary>
    /// The object's version number (increments on each modification)
    /// </summary>
    public ulong Version { get; init; }

    /// <summary>
    /// The object's digest (hash of the object's contents at this version)
    /// </summary>
    public ObjectDigest Digest { get; init; }

    /// <summary>
    /// Create an object reference
    /// </summary>
    public ObjectRef(ObjectId objectId, ulong version, ObjectDigest digest)
    {
        ObjectId = objectId;
        Version = version;
        Digest = digest;
    }

    /// <summary>
    /// Create an object reference from string representations
    /// </summary>
    public ObjectRef(string objectId, ulong version, string digest)
        : this(new ObjectId(objectId), version, new ObjectDigest(digest))
    {
    }

    /// <summary>
    /// Create the initial object reference (version 1)
    /// </summary>
    public static ObjectRef Initial(ObjectId objectId, ObjectDigest digest)
        => new(objectId, 1, digest);

    /// <summary>
    /// Create the next version of this object reference
    /// </summary>
    public ObjectRef NextVersion(ObjectDigest newDigest)
        => new(ObjectId, Version + 1, newDigest);

    /// <summary>
    /// Check if this is the initial version
    /// </summary>
    public bool IsInitialVersion => Version == 1;

    /// <summary>
    /// Convert to a compact string representation
    /// </summary>
    public override string ToString() => 
        $"{ObjectId.ToShortString()}:v{Version}:{Digest.ToShortString()}";

    /// <summary>
    /// Convert to a detailed string representation
    /// </summary>
    public string ToDetailedString() => 
        $"ObjectRef(id={ObjectId}, version={Version}, digest={Digest})";

    /// <summary>
    /// Serialize object reference using BCS encoding
    /// </summary>
    public byte[] Serialize()
    {
        var writer = new BcsWriter();
        
        // Serialize object ID
        Bcs.Vector(Bcs.U8).Write(ObjectId.Bytes, writer);
        
        // Serialize version
        Bcs.U64.Write(Version, writer);
        
        // Serialize digest
        Bcs.Vector(Bcs.U8).Write(Digest.Bytes, writer);
        
        return writer.ToBytes();
    }

    /// <summary>
    /// Deserialize object reference from BCS encoding
    /// </summary>
    public static ObjectRef Deserialize(byte[] data)
    {
        var reader = new BcsReader(data);
        return Deserialize(reader);
    }

    /// <summary>
    /// Deserialize object reference from BCS reader
    /// </summary>
    public static ObjectRef Deserialize(BcsReader reader)
    {
        // Read object ID 
        var objectIdBytes = Bcs.Vector(Bcs.U8).Read(reader);
        var objectId = new ObjectId(objectIdBytes);
        
        // Read version
        var version = Bcs.U64.Read(reader);
        
        // Read digest
        var digestBytes = Bcs.Vector(Bcs.U8).Read(reader);
        var digest = new ObjectDigest(digestBytes);
        
        return new ObjectRef(objectId, version, digest);
    }

    /// <summary>
    /// Check if two object references refer to the same object
    /// (same ID, ignoring version and digest)
    /// </summary>
    public bool RefersToSameObject(ObjectRef other) => ObjectId == other.ObjectId;

    /// <summary>
    /// Check if this reference is newer than another
    /// </summary>
    public bool IsNewerThan(ObjectRef other)
    {
        if (!RefersToSameObject(other))
            throw new ArgumentException("Cannot compare versions of different objects");
        
        return Version > other.Version;
    }
}