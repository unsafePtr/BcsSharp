using System.Diagnostics;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using NSec.Cryptography;

namespace Sui.Types;

/// <summary>
/// Represents a Sui object identifier - a unique 32-byte identifier for objects
/// Object IDs are derived from transaction digests and creation context
/// </summary>
[DebuggerDisplay("{ToString()}")]
public readonly struct ObjectId : IEquatable<ObjectId>, IComparable<ObjectId>
{
    /// <summary>
    /// The raw 32-byte object ID data
    /// </summary>
    public readonly byte[] Bytes;

    /// <summary>
    /// Standard Sui object ID length in bytes
    /// </summary>
    public const int ObjectIdLength = 32;

    /// <summary>
    /// Create object ID from 32-byte array
    /// </summary>
    public ObjectId(byte[] bytes)
    {
        if (bytes.Length != ObjectIdLength)
            throw new ArgumentException($"Object ID must be exactly {ObjectIdLength} bytes", nameof(bytes));
        
        Bytes = new byte[ObjectIdLength];
        bytes.CopyTo(Bytes, 0);
    }

    /// <summary>
    /// Create object ID from hex string (with or without 0x prefix)
    /// </summary>
    public ObjectId(string hexString)
    {
        var cleanHex = hexString.StartsWith("0x") ? hexString[2..] : hexString;
        
        // Pad with leading zeros if needed
        cleanHex = cleanHex.PadLeft(ObjectIdLength * 2, '0');
        
        if (cleanHex.Length != ObjectIdLength * 2)
            throw new ArgumentException($"Invalid hex string length. Expected {ObjectIdLength * 2} characters", nameof(hexString));

        Bytes = Convert.FromHexString(cleanHex);
    }

    /// <summary>
    /// Create object ID from SuiAddress (addresses and object IDs share the same format)
    /// </summary>
    public ObjectId(SuiAddress address)
    {
        Bytes = new byte[ObjectIdLength];
        address.Bytes.CopyTo(Bytes, 0);
    }

    /// <summary>
    /// Derive object ID from transaction digest and creation index
    /// </summary>
    public static ObjectId FromTransactionDigest(TransactionDigest digest, uint creationIndex)
    {
        // Object ID derivation: Blake2b-256(transaction_digest || creation_index_as_u64_le)
        var input = new byte[digest.Bytes.Length + 8];
        digest.Bytes.CopyTo(input, 0);
        BitConverter.GetBytes((ulong)creationIndex).CopyTo(input, digest.Bytes.Length);

        var output = new byte[ObjectIdLength];
        var blake2b = HashAlgorithm.Blake2b_256;
        blake2b.Hash(input, output);
        return new ObjectId(output);
    }

    /// <summary>
    /// Generate a random object ID (for testing purposes)
    /// </summary>
    public static ObjectId Random()
    {
        var bytes = new byte[ObjectIdLength];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return new ObjectId(bytes);
    }

    /// <summary>
    /// Zero object ID (all zeros)
    /// </summary>
    public static ObjectId Zero => new(new byte[ObjectIdLength]);

    /// <summary>
    /// System clock object ID
    /// </summary>
    public static ObjectId Clock => new("0x0000000000000000000000000000000000000000000000000000000000000006");

    /// <summary>
    /// System random generator object ID  
    /// </summary>
    public static ObjectId SystemRandom => new("0x0000000000000000000000000000000000000000000000000000000000000008");

    /// <summary>
    /// Convert to hex string with 0x prefix
    /// </summary>
    public override string ToString() => "0x" + Convert.ToHexString(Bytes).ToLowerInvariant();

    /// <summary>
    /// Convert to short hex string (first 8 characters after 0x)
    /// </summary>
    public string ToShortString() => ToString()[..10];

    /// <summary>
    /// Convert to SuiAddress (same underlying format)
    /// </summary>
    public SuiAddress ToAddress() => new(Bytes);

    /// <summary>
    /// Serialize object ID using BCS encoding
    /// </summary>
    public byte[] Serialize()
    {
        var writer = new BcsWriter();
        Bcs.Vector(Bcs.U8).Write(Bytes, writer);
        return writer.ToBytes();
    }

    /// <summary>
    /// Deserialize object ID from BCS encoding
    /// </summary>
    public static ObjectId Deserialize(byte[] data)
    {
        var reader = new BcsReader(data);
        var bytes = Bcs.Vector(Bcs.U8).Read(reader);
        return new ObjectId(bytes);
    }

    /// <summary>
    /// Deserialize object ID from BCS reader
    /// </summary>
    public static ObjectId Deserialize(BcsReader reader)
    {
        var bytes = Bcs.Vector(Bcs.U8).Read(reader);
        return new ObjectId(bytes);
    }

    public bool Equals(ObjectId other) => Bytes.SequenceEqual(other.Bytes);

    public override bool Equals(object? obj) => obj is ObjectId other && Equals(other);

    public override int GetHashCode() => BitConverter.ToInt32(Bytes, 0);

    public int CompareTo(ObjectId other)
    {
        for (int i = 0; i < ObjectIdLength; i++)
        {
            var result = Bytes[i].CompareTo(other.Bytes[i]);
            if (result != 0) return result;
        }
        return 0;
    }

    public static bool operator ==(ObjectId left, ObjectId right) => left.Equals(right);
    public static bool operator !=(ObjectId left, ObjectId right) => !left.Equals(right);
    public static bool operator <(ObjectId left, ObjectId right) => left.CompareTo(right) < 0;
    public static bool operator >(ObjectId left, ObjectId right) => left.CompareTo(right) > 0;
    public static bool operator <=(ObjectId left, ObjectId right) => left.CompareTo(right) <= 0;
    public static bool operator >=(ObjectId left, ObjectId right) => left.CompareTo(right) >= 0;

    public static implicit operator string(ObjectId objectId) => objectId.ToString();
    public static implicit operator ObjectId(string hexString) => new(hexString);
    public static implicit operator SuiAddress(ObjectId objectId) => objectId.ToAddress();
    public static implicit operator ObjectId(SuiAddress address) => new(address);
}