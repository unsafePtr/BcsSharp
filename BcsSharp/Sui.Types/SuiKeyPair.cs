using System.Diagnostics;

namespace Sui.Types;

/// <summary>
/// Abstract base class for Sui key pairs
/// Provides common functionality and defines the contract for different signature schemes
/// </summary>
[DebuggerDisplay("{ToString()}")]
public abstract class SuiKeyPair : DisposableSigner
{
    /// <summary>
    /// The Sui address derived from this key pair's public key
    /// </summary>
    public override SuiAddress Address { get; }

    /// <summary>
    /// The signature scheme used by this key pair
    /// </summary>
    public override abstract SuiSignatureScheme Scheme { get; }

    /// <summary>
    /// Protected constructor for derived classes
    /// </summary>
    protected SuiKeyPair(SuiAddress address)
    {
        Address = address;
    }

    /// <summary>
    /// Create a key pair from a base64-encoded private key with scheme prefix
    /// The first byte indicates the signature scheme:
    /// - 0x00: Ed25519 (supported)
    /// - 0x01: ECDSA Secp256k1 (not implemented)
    /// - 0x02: ECDSA Secp256r1 (not implemented)
    /// 
    /// Currently only Ed25519 is supported.
    /// </summary>
    /// <param name="base64Key">Base64 encoded key with scheme prefix</param>
    /// <returns>Ed25519KeyPair instance</returns>
    /// <exception cref="NotSupportedException">Thrown for unsupported signature schemes</exception>
    public static SuiKeyPair FromBase64PrivateKey(string base64Key)
    {
        var keyData = Convert.FromBase64String(base64Key);
        
        if (keyData.Length < 2)
            throw new ArgumentException("Private key data is too short", nameof(base64Key));

        var schemeFlag = keyData[0];
        var scheme = SuiSignatureSchemeExtensions.FromFlag(schemeFlag);
        var privateKeyBytes = keyData.AsSpan(1).ToArray();

        return scheme switch
        {
            SuiSignatureScheme.Ed25519 => Ed25519KeyPair.FromPrivateKeyBytes(privateKeyBytes),
            SuiSignatureScheme.EcdsaSecp256k1 => throw new NotSupportedException("ECDSA Secp256k1 is not implemented. Only Ed25519 is currently supported."),
            SuiSignatureScheme.EcdsaSecp256r1 => throw new NotSupportedException("ECDSA Secp256r1 is not implemented. Only Ed25519 is currently supported."),
            _ => throw new ArgumentException($"Unknown signature scheme: {scheme}. Only Ed25519 is currently supported.")
        };
    }

    /// <summary>
    /// Generate a new key pair for the specified signature scheme
    /// Currently only Ed25519 is supported.
    /// </summary>
    /// <param name="scheme">The signature scheme to use (only Ed25519 supported)</param>
    /// <returns>New Ed25519KeyPair instance</returns>
    /// <exception cref="NotSupportedException">Thrown for unsupported signature schemes</exception>
    public static SuiKeyPair Generate(SuiSignatureScheme scheme = SuiSignatureScheme.Ed25519)
    {
        return scheme switch
        {
            SuiSignatureScheme.Ed25519 => Ed25519KeyPair.Generate(),
            SuiSignatureScheme.EcdsaSecp256k1 => throw new NotSupportedException("ECDSA Secp256k1 is not implemented. Only Ed25519 is currently supported."),
            SuiSignatureScheme.EcdsaSecp256r1 => throw new NotSupportedException("ECDSA Secp256r1 is not implemented. Only Ed25519 is currently supported."),
            _ => throw new ArgumentException($"Unknown signature scheme: {scheme}. Only Ed25519 is currently supported.")
        };
    }

    /// <summary>
    /// Generate a new Ed25519 key pair (convenience method)
    /// </summary>
    /// <returns>New Ed25519 key pair</returns>
    public static SuiKeyPair Generate() => Generate(SuiSignatureScheme.Ed25519);

    /// <summary>
    /// Create a key pair from a private key seed
    /// Currently only Ed25519 is supported.
    /// </summary>
    /// <param name="seed">Private key seed bytes (32 bytes for Ed25519)</param>
    /// <param name="scheme">Signature scheme (only Ed25519 supported)</param>
    /// <returns>Ed25519KeyPair instance</returns>
    /// <exception cref="NotSupportedException">Thrown for unsupported signature schemes</exception>
    public static SuiKeyPair FromSeed(byte[] seed, SuiSignatureScheme scheme = SuiSignatureScheme.Ed25519)
    {
        return scheme switch
        {
            SuiSignatureScheme.Ed25519 => Ed25519KeyPair.FromSeed(seed),
            SuiSignatureScheme.EcdsaSecp256k1 => throw new NotSupportedException("ECDSA Secp256k1 is not implemented. Only Ed25519 is currently supported."),
            SuiSignatureScheme.EcdsaSecp256r1 => throw new NotSupportedException("ECDSA Secp256r1 is not implemented. Only Ed25519 is currently supported."),
            _ => throw new ArgumentException($"Unknown signature scheme: {scheme}. Only Ed25519 is currently supported.")
        };
    }

    /// <summary>
    /// Create a key pair from a hex-encoded private key seed
    /// Currently only Ed25519 is supported.
    /// </summary>
    /// <param name="hexSeed">Hex encoded seed (64 hex chars for Ed25519)</param>
    /// <param name="scheme">Signature scheme (only Ed25519 supported)</param>
    /// <returns>Ed25519KeyPair instance</returns>
    /// <exception cref="NotSupportedException">Thrown for unsupported signature schemes</exception>
    public static SuiKeyPair FromSeed(string hexSeed, SuiSignatureScheme scheme = SuiSignatureScheme.Ed25519)
    {
        var seed = Convert.FromHexString(hexSeed);
        return FromSeed(seed, scheme);
    }

    /// <summary>
    /// Export private key with scheme prefix as base64
    /// Format: [scheme_flag(1) || private_key_bytes(varies)]
    /// </summary>
    /// <returns>Base64 string with scheme prefix</returns>
    public override string ExportPrivateKeyBase64()
    {
        var privateKeyBytes = ExportPrivateKey();
        var keyWithScheme = new byte[privateKeyBytes.Length + 1];
        keyWithScheme[0] = (byte)Scheme;
        privateKeyBytes.CopyTo(keyWithScheme, 1);
        return Convert.ToBase64String(keyWithScheme);
    }

    /// <summary>
    /// Export public key as hex string
    /// </summary>
    /// <returns>Public key hex string</returns>
    public string ExportPublicKeyHex()
    {
        return Convert.ToHexString(ExportPublicKey()).ToLowerInvariant();
    }

    /// <summary>
    /// Generate the Sui address from this key pair's public key
    /// This method explicitly shows the address generation process
    /// </summary>
    /// <returns>Sui address derived from the public key and signature scheme</returns>
    public SuiAddress GenerateAddress()
    {
        var publicKeyBytes = ExportPublicKey();
        return SuiAddress.FromPublicKeyBytes(publicKeyBytes, Scheme);
    }

    /// <inheritdoc />
    public override string ToString() => $"SuiKeyPair({Scheme}, address={Address})";
}