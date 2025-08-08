using System.Diagnostics;
using NSec.Cryptography;
using System.Text;

namespace Sui.Types;

/// <summary>
/// Represents a Sui signature containing the signature bytes, public key, and scheme
/// Sui signatures support multiple cryptographic schemes: Ed25519, ECDSA Secp256k1, ECDSA Secp256r1
/// </summary>
[DebuggerDisplay("{ToString()}")]
public readonly struct SuiSignature : IEquatable<SuiSignature>
{
    /// <summary>
    /// The raw signature bytes (64 bytes for all supported schemes)
    /// </summary>
    public readonly byte[] SignatureBytes;

    /// <summary>
    /// The public key bytes used to create this signature
    /// Length varies by scheme: Ed25519=32, ECDSA compressed=33, uncompressed=65
    /// </summary>
    public readonly byte[] PublicKeyBytes;

    /// <summary>
    /// The signature scheme used for this signature
    /// </summary>
    public readonly SuiSignatureScheme Scheme;

    /// <summary>
    /// NSec PublicKey for Ed25519 signatures (null for ECDSA schemes)
    /// </summary>
    public readonly PublicKey? NSecPublicKey;

    /// <summary>
    /// Create an Ed25519 signature from signature bytes and NSec PublicKey
    /// </summary>
    public SuiSignature(byte[] signatureBytes, PublicKey publicKey)
    {
        if (publicKey.Algorithm != SignatureAlgorithm.Ed25519)
            throw new ArgumentException("PublicKey must be Ed25519 for this constructor", nameof(publicKey));

        if (signatureBytes.Length != 64)
            throw new ArgumentException($"Signature must be 64 bytes, got {signatureBytes.Length}", nameof(signatureBytes));

        SignatureBytes = new byte[64];
        signatureBytes.CopyTo(SignatureBytes, 0);

        PublicKeyBytes = publicKey.Export(KeyBlobFormat.RawPublicKey);
        Scheme = SuiSignatureScheme.Ed25519;
        NSecPublicKey = publicKey;
    }

    /// <summary>
    /// Create a signature from signature bytes, public key bytes, and scheme
    /// This constructor supports all signature schemes including ECDSA
    /// </summary>
    public SuiSignature(byte[] signatureBytes, byte[] publicKeyBytes, SuiSignatureScheme scheme)
    {
        if (signatureBytes.Length != scheme.GetSignatureLength())
            throw new ArgumentException($"{scheme} signature must be {scheme.GetSignatureLength()} bytes, got {signatureBytes.Length}", nameof(signatureBytes));

        // Validate public key length for the scheme
        var compressedLength = scheme.GetCompressedPublicKeyLength();
        var uncompressedLength = scheme.GetUncompressedPublicKeyLength();

        if (publicKeyBytes.Length != compressedLength && publicKeyBytes.Length != uncompressedLength)
            throw new ArgumentException($"{scheme} public key must be {compressedLength} (compressed) or {uncompressedLength} (uncompressed) bytes, got {publicKeyBytes.Length}", nameof(publicKeyBytes));

        SignatureBytes = new byte[signatureBytes.Length];
        signatureBytes.CopyTo(SignatureBytes, 0);

        PublicKeyBytes = new byte[publicKeyBytes.Length];
        publicKeyBytes.CopyTo(PublicKeyBytes, 0);

        Scheme = scheme;

        // Create NSec PublicKey only for Ed25519
        NSecPublicKey = scheme == SuiSignatureScheme.Ed25519
            ? PublicKey.Import(SignatureAlgorithm.Ed25519, publicKeyBytes, KeyBlobFormat.RawPublicKey)
            : null;
    }

    /// <summary>
    /// Parse a signature from Sui's serialized format:
    /// [scheme_flag(1) || signature_bytes(64) || public_key_bytes(varies)]
    /// </summary>
    public static SuiSignature FromSuiBytes(byte[] suiSignatureBytes)
    {
        if (suiSignatureBytes.Length < 66) // Minimum: 64 (sig) + 1 (flag) + 1 (min key)
            throw new ArgumentException($"Sui signature too short: {suiSignatureBytes.Length} bytes", nameof(suiSignatureBytes));

        var schemeFlag = suiSignatureBytes[0]; // First byte is scheme flag
        var scheme = SuiSignatureSchemeExtensions.FromFlag(schemeFlag);

        var signatureBytes = suiSignatureBytes.AsSpan(1, 64).ToArray();
        var publicKeyBytes = suiSignatureBytes.AsSpan(65).ToArray();

        return new SuiSignature(signatureBytes, publicKeyBytes, scheme);
    }

    /// <summary>
    /// Parse a signature from base64 string
    /// </summary>
    public static SuiSignature FromBase64(string base64Signature)
    {
        var bytes = Convert.FromBase64String(base64Signature);
        return FromSuiBytes(bytes);
    }

    /// <summary>
    /// Parse a signature from hex string
    /// </summary>
    public static SuiSignature FromHex(string hexSignature)
    {
        var cleanHex = hexSignature.StartsWith("0x") ? hexSignature[2..] : hexSignature;
        var bytes = Convert.FromHexString(cleanHex);
        return FromSuiBytes(bytes);
    }

    /// <summary>
    /// Serialize to Sui's signature format:
    /// [scheme_flag(1) || signature_bytes(64) || public_key_bytes(varies)]
    /// </summary>
    public byte[] ToSuiBytes()
    {
        var result = new byte[64 + PublicKeyBytes.Length + 1];

        result[0] = (byte)Scheme;
        SignatureBytes.CopyTo(result.AsSpan(1, 64));
        PublicKeyBytes.CopyTo(result.AsSpan(65, PublicKeyBytes.Length));

        return result;
    }

    /// <summary>
    /// Convert to base64 string
    /// </summary>
    public string ToBase64() => Convert.ToBase64String(ToSuiBytes());

    /// <summary>
    /// Convert to hex string with 0x prefix
    /// </summary>
    public string ToHex() => "0x" + Convert.ToHexString(ToSuiBytes()).ToLowerInvariant();

    /// <summary>
    /// Get the Sui address that created this signature
    /// Currently only supports Ed25519 addresses
    /// </summary>
    public SuiAddress GetSignerAddress()
    {
        if (Scheme != SuiSignatureScheme.Ed25519)
            throw new NotSupportedException($"Address derivation for {Scheme} not yet implemented. Only Ed25519 is currently supported.");

        if (NSecPublicKey == null)
            throw new InvalidOperationException("NSec PublicKey is null for Ed25519 signature");

        return SuiAddress.FromPublicKey(NSecPublicKey);
    }

    /// <summary>
    /// Verify this signature against the given data
    /// Currently only supports Ed25519 verification
    /// </summary>
    public bool Verify(byte[] data)
    {
        if (Scheme != SuiSignatureScheme.Ed25519)
            throw new NotSupportedException($"Signature verification for {Scheme} not yet implemented. Only Ed25519 is currently supported.");

        if (NSecPublicKey == null)
            throw new InvalidOperationException("NSec PublicKey is null for Ed25519 signature");

        var algorithm = SignatureAlgorithm.Ed25519;
        return algorithm.Verify(NSecPublicKey, data, SignatureBytes);
    }

    /// <summary>
    /// Verify this signature against a transaction digest with intent
    /// </summary>
    public bool VerifyTransaction(TransactionDigest digest, IntentScope scope = IntentScope.TransactionData)
    {
        var intentMessage = CreateIntentMessage(digest.Bytes, scope);
        return Verify(intentMessage);
    }

    /// <summary>
    /// Create intent message for verification (as per Sui specification)
    /// </summary>
    private static byte[] CreateIntentMessage(byte[] data, IntentScope scope)
    {
        // Intent: [scope, version, app_id]
        var intent = new byte[] { (byte)scope, 0, 0 }; // version=0, app_id=0 for Sui

        // Intent message: intent || data
        var intentMessage = new byte[intent.Length + data.Length];
        intent.CopyTo(intentMessage, 0);
        data.CopyTo(intentMessage, intent.Length);

        return intentMessage;
    }

    public bool Equals(SuiSignature other)
    {
        return SignatureBytes.SequenceEqual(other.SignatureBytes) &&
               PublicKeyBytes.SequenceEqual(other.PublicKeyBytes) &&
               Scheme == other.Scheme;
    }

    public override bool Equals(object? obj) => obj is SuiSignature other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(
        BitConverter.ToInt32(SignatureBytes, 0),
        BitConverter.ToInt32(PublicKeyBytes, 0),
        Scheme);

    public static bool operator ==(SuiSignature left, SuiSignature right) => left.Equals(right);
    public static bool operator !=(SuiSignature left, SuiSignature right) => !left.Equals(right);

    public override string ToString() => $"SuiSignature({Scheme}, {ToBase64()[..16]}...)";
}