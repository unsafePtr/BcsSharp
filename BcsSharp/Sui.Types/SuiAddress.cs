using System.Diagnostics;
using System.Text;
using BcsSharp.Core;
using BcsSharp.Core.Types;
using NSec.Cryptography;

namespace Sui.Types;

/// <summary>
/// Represents a Sui address - a 32-byte identifier for accounts and objects
/// Sui addresses are derived from public keys using Blake2b-256 hash
/// </summary>
[DebuggerDisplay("{ToString()}")]
public readonly struct SuiAddress : IEquatable<SuiAddress>, IComparable<SuiAddress>
{
    /// <summary>
    /// The raw 32-byte address data
    /// </summary>
    public readonly byte[] Bytes;

    /// <summary>
    /// Standard Sui address length in bytes
    /// </summary>
    public const int AddressLength = 32;

    /// <summary>
    /// Create address from 32-byte array
    /// </summary>
    public SuiAddress(byte[] bytes)
    {
        if (bytes.Length != AddressLength)
            throw new ArgumentException($"Address must be exactly {AddressLength} bytes", nameof(bytes));

        Bytes = new byte[AddressLength];
        bytes.CopyTo(Bytes, 0);
    }

    /// <summary>
    /// Create address from hex string (with or without 0x prefix)
    /// Supports both standard format (64 hex chars) and scheme-aware format (66 hex chars)
    /// </summary>
    public SuiAddress(string addressString)
    {
        if (string.IsNullOrEmpty(addressString))
            throw new ArgumentException("Address string cannot be null or empty", nameof(addressString));

        string hexPart;
        if (addressString.StartsWith("0x"))
        {
            hexPart = addressString[2..];
            if (string.IsNullOrEmpty(hexPart))
                throw new ArgumentException("Invalid address format - missing hex part after 0x", nameof(addressString));
        }
        else
        {
            hexPart = addressString;
        }

        // Validate hex characters
        if (!hexPart.All(c => "0123456789abcdefABCDEF".Contains(c)))
            throw new ArgumentException("Invalid address format. String contains non-hex characters", nameof(addressString));

        // Check if this is a scheme-aware address format (33 bytes = 66 hex chars)
        if (hexPart.Length == (AddressLength + 1) * 2) // 66 hex chars = 33 bytes
        {
            try
            {
                var fullBytes = Convert.FromHexString(hexPart);
                // Extract just the address bytes (bytes 1-32), ignore the scheme byte (byte 0)
                Bytes = new byte[AddressLength];
                Array.Copy(fullBytes, 1, Bytes, 0, AddressLength);
            }
            catch
            {
                throw new ArgumentException("Invalid hex string format for scheme-aware address", nameof(addressString));
            }
        }
        else
        {
            // Standard address format (32 bytes = 64 hex chars or less)
            // Pad with leading zeros if needed (addresses can be shortened)
            var cleanHex = hexPart.PadLeft(AddressLength * 2, '0');
            if (cleanHex.Length != AddressLength * 2)
                throw new ArgumentException($"Invalid address length. Expected up to {AddressLength * 2} hex characters (standard) or {(AddressLength + 1) * 2} hex characters (scheme-aware)", nameof(addressString));

            try
            {
                Bytes = Convert.FromHexString(cleanHex);
            }
            catch
            {
                throw new ArgumentException("Invalid hex string format", nameof(addressString));
            }
        }
    }

    /// <summary>
    /// Derive Sui address from Ed25519 public key using Blake2b-256
    /// This method is kept for backward compatibility but delegates to the generic method
    /// </summary>
    public static SuiAddress FromPublicKey(PublicKey publicKey)
    {
        if (publicKey.Algorithm != SignatureAlgorithm.Ed25519)
            throw new ArgumentException("Only Ed25519 public keys are supported", nameof(publicKey));

        var keyBytes = publicKey.Export(KeyBlobFormat.RawPublicKey);
        return FromPublicKeyBytes(keyBytes, SuiSignatureScheme.Ed25519);
    }

    /// <summary>
    /// Derive Sui address from any ISigner implementation
    /// This is the recommended way to derive addresses as it supports all signature schemes
    /// </summary>
    public static SuiAddress FromSigner(ISigner signer)
    {
        var publicKeyBytes = signer.ExportPublicKey();
        return FromPublicKeyBytes(publicKeyBytes, signer.Scheme);
    }

    /// <summary>
    /// Derive Sui address from public key bytes and signature scheme
    /// Sui address derivation: hash(public_key_bytes || scheme_flag)
    /// - Ed25519: Blake2b-256(public_key || 0x00)
    /// - ECDSA schemes: Blake2b-256(public_key || scheme_flag) 
    /// </summary>
    public static SuiAddress FromPublicKeyBytes(byte[] publicKeyBytes, SuiSignatureScheme scheme)
    {
        if (publicKeyBytes == null)
            throw new ArgumentNullException(nameof(publicKeyBytes));

        // Validate public key length
        var compressedLength = scheme.GetCompressedPublicKeyLength();
        var uncompressedLength = scheme.GetUncompressedPublicKeyLength();

        if (publicKeyBytes.Length != compressedLength && publicKeyBytes.Length != uncompressedLength)
            throw new ArgumentException($"{scheme} public key must be {compressedLength} (compressed) or {uncompressedLength} (uncompressed) bytes, got {publicKeyBytes.Length}", nameof(publicKeyBytes));

        // Create input: public_key_bytes || scheme_flag
        var input = new byte[publicKeyBytes.Length + 1];
        input[0] = (byte)scheme;
        publicKeyBytes.CopyTo(input.AsSpan(1, publicKeyBytes.Length));

        // All Sui addresses use Blake2b-256 regardless of signature scheme
        var output = new byte[AddressLength];
        var blake2b = HashAlgorithm.Blake2b_256;
        blake2b.Hash(input, output);
        return new SuiAddress(output);
    }

    /// <summary>
    /// Create address from bytes
    /// </summary>
    private static SuiAddress FromBytes(byte[] bytes) => new(bytes);

    /// <summary>
    /// Zero address (all zeros)
    /// </summary>
    public static SuiAddress Zero => new(new byte[AddressLength]);

    /// <summary>
    /// System state object address
    /// </summary>
    public static SuiAddress SystemState => FromBytes(new byte[AddressLength - 1].Concat(new byte[] { 0x05 }).ToArray());

    /// <summary>
    /// SUI coin type address
    /// </summary>
    public static SuiAddress SuiFramework => FromBytes(new byte[AddressLength - 1].Concat(new byte[] { 0x02 }).ToArray());

    /// <summary>
    /// Convert to hex string with 0x prefix (standard Sui address format)
    /// </summary>
    public override string ToString() => "0x" + Convert.ToHexString(Bytes).ToLowerInvariant();

    /// <summary>
    /// Convert to hex string with 0x prefix (alternative format)
    /// </summary>
    public string ToHexString() => "0x" + Convert.ToHexString(Bytes).ToLowerInvariant();

    /// <summary>
    /// Convert to short hex string (first 8 hex characters after 0x)
    /// </summary>
    public string ToShortString() => ToString()[..Math.Min(10, ToString().Length)];

    /// <summary>
    /// Convert to scheme-aware hex string including the signature scheme used to derive this address
    /// Format: 0x[scheme_flag(1) || address_bytes(32)]
    /// </summary>
    /// <param name="scheme">The signature scheme used to derive this address</param>
    /// <returns>Hex string with embedded scheme information (66 hex chars)</returns>
    public string ToSchemeAwareString(SuiSignatureScheme scheme)
    {
        var addressWithScheme = new byte[AddressLength + 1];
        addressWithScheme[0] = (byte)scheme;
        Bytes.CopyTo(addressWithScheme.AsSpan(1, AddressLength));
        return "0x" + Convert.ToHexString(addressWithScheme).ToLowerInvariant();
    }

    /// <summary>
    /// Parse a scheme-aware address string and return both the address and the signature scheme
    /// </summary>
    /// <param name="schemeAwareAddressString">Hex string containing address and scheme (66 hex chars)</param>
    /// <returns>Tuple of (address, scheme)</returns>
    /// <exception cref="ArgumentException">If the string is not in the correct scheme-aware format</exception>
    public static (SuiAddress address, SuiSignatureScheme scheme) FromSchemeAwareString(string schemeAwareAddressString)
    {
        if (string.IsNullOrEmpty(schemeAwareAddressString))
            throw new ArgumentException("Scheme-aware address string cannot be null or empty", nameof(schemeAwareAddressString));

        string hexPart;
        if (schemeAwareAddressString.StartsWith("0x"))
        {
            hexPart = schemeAwareAddressString[2..];
        }
        else
        {
            hexPart = schemeAwareAddressString;
        }

        // Must be exactly 66 hex characters (33 bytes)
        if (hexPart.Length != (AddressLength + 1) * 2)
            throw new ArgumentException($"Scheme-aware address must be exactly {(AddressLength + 1) * 2} hex characters", nameof(schemeAwareAddressString));

        // Validate hex characters
        if (!hexPart.All(c => "0123456789abcdefABCDEF".Contains(c)))
            throw new ArgumentException("Invalid hex characters in scheme-aware address", nameof(schemeAwareAddressString));

        try
        {
            var fullBytes = Convert.FromHexString(hexPart);

            // Extract address (first 32 bytes)
            var addressBytes = new byte[AddressLength];
            Array.Copy(fullBytes, 0, addressBytes, 0, AddressLength);

            // Extract scheme (last byte)
            var schemeFlag = fullBytes[AddressLength];
            var scheme = SuiSignatureSchemeExtensions.FromFlag(schemeFlag);

            return (new SuiAddress(addressBytes), scheme);
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            throw new ArgumentException($"Failed to parse scheme-aware address: {ex.Message}", nameof(schemeAwareAddressString));
        }
    }

    /// <summary>
    /// Serialize address using BCS encoding
    /// </summary>
    public byte[] Serialize()
    {
        var writer = new BcsWriter();
        Bcs.Vector(Bcs.U8).Write(Bytes, writer);
        return writer.ToBytes();
    }

    /// <summary>
    /// Deserialize address from BCS encoding
    /// </summary>
    public static SuiAddress Deserialize(byte[] data)
    {
        var reader = new BcsReader(data);
        var bytes = Bcs.Vector(Bcs.U8).Read(reader);
        return new SuiAddress(bytes);
    }

    /// <summary>
    /// Deserialize address from BCS reader
    /// </summary>
    public static SuiAddress Deserialize(BcsReader reader)
    {
        var bytes = Bcs.Vector(Bcs.U8).Read(reader);
        return new SuiAddress(bytes);
    }

    public bool Equals(SuiAddress other) => Bytes.SequenceEqual(other.Bytes);

    public override bool Equals(object? obj) => obj is SuiAddress other && Equals(other);

    public override int GetHashCode() => BitConverter.ToInt32(Bytes, 0);

    public int CompareTo(SuiAddress other)
    {
        for (int i = 0; i < AddressLength; i++)
        {
            var result = Bytes[i].CompareTo(other.Bytes[i]);
            if (result != 0) return result;
        }
        return 0;
    }

    public static bool operator ==(SuiAddress left, SuiAddress right) => left.Equals(right);
    public static bool operator !=(SuiAddress left, SuiAddress right) => !left.Equals(right);
    public static bool operator <(SuiAddress left, SuiAddress right) => left.CompareTo(right) < 0;
    public static bool operator >(SuiAddress left, SuiAddress right) => left.CompareTo(right) > 0;
    public static bool operator <=(SuiAddress left, SuiAddress right) => left.CompareTo(right) <= 0;
    public static bool operator >=(SuiAddress left, SuiAddress right) => left.CompareTo(right) >= 0;

    public static implicit operator string(SuiAddress address) => address.ToString();
    public static implicit operator SuiAddress(string hexString) => new(hexString);
}