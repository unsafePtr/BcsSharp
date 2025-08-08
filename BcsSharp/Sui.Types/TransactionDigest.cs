using System.Diagnostics;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using NSec.Cryptography;

namespace Sui.Types;

/// <summary>
/// Represents a Sui transaction digest - a unique identifier for transactions
/// Transaction digests are Blake2b-256 hashes of the transaction data
/// </summary>
[DebuggerDisplay("{ToString()}")]
public readonly struct TransactionDigest : IEquatable<TransactionDigest>, IComparable<TransactionDigest>
{
    /// <summary>
    /// The raw 32-byte digest data
    /// </summary>
    public readonly byte[] Bytes;

    /// <summary>
    /// Standard Sui transaction digest length in bytes
    /// </summary>
    public const int DigestLength = 32;

    /// <summary>
    /// Create digest from 32-byte array
    /// </summary>
    public TransactionDigest(byte[] bytes)
    {
        if (bytes.Length != DigestLength)
            throw new ArgumentException($"Transaction digest must be exactly {DigestLength} bytes", nameof(bytes));

        Bytes = new byte[DigestLength];
        bytes.CopyTo(Bytes, 0);
    }

    /// <summary>
    /// Create digest from Base58 string
    /// </summary>
    public TransactionDigest(string base58String)
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
    /// Compute transaction digest from transaction data using Blake2b-256
    /// </summary>
    public static TransactionDigest FromTransactionData(byte[] transactionData)
    {
        var output = new byte[DigestLength];
        var blake2b = HashAlgorithm.Blake2b_256;
        blake2b.Hash(transactionData, output);
        return new TransactionDigest(output);
    }

    /// <summary>
    /// Zero digest (all zeros)
    /// </summary>
    public static TransactionDigest Zero => new(new byte[DigestLength]);

    /// <summary>
    /// Generate a random transaction digest (FOR TESTING ONLY)
    /// In production, transaction digests should be computed from actual transaction data
    /// </summary>
    public static TransactionDigest Random()
    {
        var random = new Random();
        var bytes = new byte[DigestLength];
        random.NextBytes(bytes);
        return new TransactionDigest(bytes);
    }

    /// <summary>
    /// Convert to Base58 string (standard Sui transaction digest format)
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
    public static TransactionDigest Deserialize(byte[] data)
    {
        var reader = new BcsReader(data);
        var bytes = Bcs.Vector(Bcs.U8).Read(reader);
        return new TransactionDigest(bytes);
    }

    /// <summary>
    /// Deserialize digest from BCS reader
    /// </summary>
    public static TransactionDigest Deserialize(BcsReader reader)
    {
        var bytes = Bcs.Vector(Bcs.U8).Read(reader);
        return new TransactionDigest(bytes);
    }

    public bool Equals(TransactionDigest other) => Bytes.SequenceEqual(other.Bytes);

    public override bool Equals(object? obj) => obj is TransactionDigest other && Equals(other);

    public override int GetHashCode() => BitConverter.ToInt32(Bytes, 0);

    public int CompareTo(TransactionDigest other)
    {
        for (int i = 0; i < DigestLength; i++)
        {
            var result = Bytes[i].CompareTo(other.Bytes[i]);
            if (result != 0) return result;
        }
        return 0;
    }

    public static bool operator ==(TransactionDigest left, TransactionDigest right) => left.Equals(right);
    public static bool operator !=(TransactionDigest left, TransactionDigest right) => !left.Equals(right);
    public static bool operator <(TransactionDigest left, TransactionDigest right) => left.CompareTo(right) < 0;
    public static bool operator >(TransactionDigest left, TransactionDigest right) => left.CompareTo(right) > 0;
    public static bool operator <=(TransactionDigest left, TransactionDigest right) => left.CompareTo(right) <= 0;
    public static bool operator >=(TransactionDigest left, TransactionDigest right) => left.CompareTo(right) >= 0;

    public static implicit operator string(TransactionDigest digest) => digest.ToString();
    public static implicit operator TransactionDigest(string base58String) => new(base58String);
}