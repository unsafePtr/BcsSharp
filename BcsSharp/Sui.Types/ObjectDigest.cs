using System.Diagnostics;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using NSec.Cryptography;

namespace Sui.Types;

/// <summary>
/// Represents a Sui object digest - a hash of an object's contents at a specific version
/// Object digests are Blake2b-256 hashes used for integrity verification
/// </summary>
[DebuggerDisplay("{ToString()}")]
public readonly struct ObjectDigest : IEquatable<ObjectDigest>, IComparable<ObjectDigest>
{
    /// <summary>
    /// The raw 32-byte digest data
    /// </summary>
    public readonly byte[] Bytes;

    /// <summary>
    /// Standard Sui object digest length in bytes
    /// </summary>
    public const int DigestLength = 32;

    /// <summary>
    /// Create digest from 32-byte array
    /// </summary>
    public ObjectDigest(byte[] bytes)
    {
        if (bytes.Length != DigestLength)
            throw new ArgumentException($"Object digest must be exactly {DigestLength} bytes", nameof(bytes));
        
        Bytes = new byte[DigestLength];
        bytes.CopyTo(Bytes, 0);
    }

    /// <summary>
    /// Create digest from Base58 string
    /// </summary>
    public ObjectDigest(string base58String)
    {
        if (string.IsNullOrEmpty(base58String))
            throw new ArgumentException("Digest string cannot be null or empty", nameof(base58String));

        try
        {
            Bytes = SuiBase58.Decode(base58String);
            if (Bytes.Length != DigestLength)
                throw new ArgumentException($"Invalid digest length. Expected {DigestLength} bytes, got {Bytes.Length}", nameof(base58String));
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            throw new ArgumentException($"Invalid Base58 string format: {ex.Message}", nameof(base58String));
        }
    }

    /// <summary>
    /// Compute object digest from object data using Blake2b-256
    /// </summary>
    public static ObjectDigest FromObjectData(byte[] objectData)
    {
        var output = new byte[DigestLength];
        var blake2b = HashAlgorithm.Blake2b_256;
        blake2b.Hash(objectData, output);
        return new ObjectDigest(output);
    }

    /// <summary>
    /// Generate a random digest (for testing purposes)
    /// </summary>
    public static ObjectDigest Random()
    {
        var bytes = new byte[DigestLength];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return new ObjectDigest(bytes);
    }

    /// <summary>
    /// Zero digest (all zeros)
    /// </summary>
    public static ObjectDigest Zero => new(new byte[DigestLength]);

    /// <summary>
    /// Convert to Base58 string (standard Sui object digest format)
    /// </summary>
    public override string ToString() => SuiBase58.Encode(Bytes);

    /// <summary>
    /// Convert to short Base58 string (first 8 characters)
    /// </summary>
    public string ToShortString() => ToString()[..Math.Min(8, ToString().Length)];

    /// <summary>
    /// Serialize digest using BCS encoding
    /// </summary>
    public byte[] Serialize()
    {
        var writer = new BcsWriter();
        Bcs.Vector(Bcs.U8).Write(Bytes, writer);
        return writer.ToBytes();
    }

    /// <summary>
    /// Deserialize digest from BCS encoding
    /// </summary>
    public static ObjectDigest Deserialize(byte[] data)
    {
        var reader = new BcsReader(data);
        var bytes = Bcs.Vector(Bcs.U8).Read(reader);
        return new ObjectDigest(bytes);
    }

    /// <summary>
    /// Deserialize digest from BCS reader
    /// </summary>
    public static ObjectDigest Deserialize(BcsReader reader)
    {
        var bytes = Bcs.Vector(Bcs.U8).Read(reader);
        return new ObjectDigest(bytes);
    }

    public bool Equals(ObjectDigest other) => Bytes.SequenceEqual(other.Bytes);

    public override bool Equals(object? obj) => obj is ObjectDigest other && Equals(other);

    public override int GetHashCode() => BitConverter.ToInt32(Bytes, 0);

    public int CompareTo(ObjectDigest other)
    {
        for (int i = 0; i < DigestLength; i++)
        {
            var result = Bytes[i].CompareTo(other.Bytes[i]);
            if (result != 0) return result;
        }
        return 0;
    }

    public static bool operator ==(ObjectDigest left, ObjectDigest right) => left.Equals(right);
    public static bool operator !=(ObjectDigest left, ObjectDigest right) => !left.Equals(right);
    public static bool operator <(ObjectDigest left, ObjectDigest right) => left.CompareTo(right) < 0;
    public static bool operator >(ObjectDigest left, ObjectDigest right) => left.CompareTo(right) > 0;
    public static bool operator <=(ObjectDigest left, ObjectDigest right) => left.CompareTo(right) <= 0;
    public static bool operator >=(ObjectDigest left, ObjectDigest right) => left.CompareTo(right) >= 0;

    public static implicit operator string(ObjectDigest digest) => digest.ToString();
    public static implicit operator ObjectDigest(string base58String) => new(base58String);
}