namespace Sui.Types;

/// <summary>
/// Sui signature scheme enumeration with corresponding flag bytes
/// </summary>
public enum SuiSignatureScheme : byte
{
    /// <summary>
    /// Ed25519 signature scheme (flag: 0x00)
    /// - Signature length: 64 bytes
    /// - Public key length: 32 bytes
    /// - Hash function: Blake2b (for address derivation)
    /// </summary>
    Ed25519 = 0x00,
    
    /// <summary>
    /// ECDSA Secp256k1 signature scheme (flag: 0x01) 
    /// - Signature length: 64 bytes [r(32) || s(32)]
    /// - Public key length: 33 bytes (compressed) or 65 bytes (uncompressed)
    /// - Hash function: SHA-256
    /// </summary>
    EcdsaSecp256k1 = 0x01,
    
    /// <summary>
    /// ECDSA Secp256r1 signature scheme (flag: 0x02)
    /// - Signature length: 64 bytes [r(32) || s(32)]
    /// - Public key length: 33 bytes (compressed) or 65 bytes (uncompressed)  
    /// - Hash function: SHA-256
    /// </summary>
    EcdsaSecp256r1 = 0x02
}

/// <summary>
/// Extension methods for SuiSignatureScheme
/// </summary>
public static class SuiSignatureSchemeExtensions
{
    /// <summary>
    /// Get the signature length in bytes for the scheme
    /// </summary>
    public static int GetSignatureLength(this SuiSignatureScheme scheme) => scheme switch
    {
        SuiSignatureScheme.Ed25519 => 64,
        SuiSignatureScheme.EcdsaSecp256k1 => 64,
        SuiSignatureScheme.EcdsaSecp256r1 => 64,
        _ => throw new ArgumentException($"Unknown signature scheme: {scheme}")
    };

    /// <summary>
    /// Get the compressed public key length in bytes for the scheme
    /// </summary>
    public static int GetCompressedPublicKeyLength(this SuiSignatureScheme scheme) => scheme switch
    {
        SuiSignatureScheme.Ed25519 => 32,
        SuiSignatureScheme.EcdsaSecp256k1 => 33,
        SuiSignatureScheme.EcdsaSecp256r1 => 33,
        _ => throw new ArgumentException($"Unknown signature scheme: {scheme}")
    };

    /// <summary>
    /// Get the uncompressed public key length in bytes for the scheme
    /// </summary>
    public static int GetUncompressedPublicKeyLength(this SuiSignatureScheme scheme) => scheme switch
    {
        SuiSignatureScheme.Ed25519 => 32, // Ed25519 doesn't have compression
        SuiSignatureScheme.EcdsaSecp256k1 => 65,
        SuiSignatureScheme.EcdsaSecp256r1 => 65,
        _ => throw new ArgumentException($"Unknown signature scheme: {scheme}")
    };

    /// <summary>
    /// Check if this scheme supports compressed public keys
    /// </summary>
    public static bool SupportsCompression(this SuiSignatureScheme scheme) => scheme switch
    {
        SuiSignatureScheme.Ed25519 => false,
        SuiSignatureScheme.EcdsaSecp256k1 => true,
        SuiSignatureScheme.EcdsaSecp256r1 => true,
        _ => throw new ArgumentException($"Unknown signature scheme: {scheme}")
    };

    /// <summary>
    /// Get the hash algorithm name used by this scheme for verification
    /// </summary>
    public static string GetHashAlgorithm(this SuiSignatureScheme scheme) => scheme switch
    {
        SuiSignatureScheme.Ed25519 => "Blake2b", // For address derivation
        SuiSignatureScheme.EcdsaSecp256k1 => "SHA256",
        SuiSignatureScheme.EcdsaSecp256r1 => "SHA256",
        _ => throw new ArgumentException($"Unknown signature scheme: {scheme}")
    };

    /// <summary>
    /// Get the BIP-32 purpose level for wallet derivation
    /// </summary>
    public static int GetBip32Purpose(this SuiSignatureScheme scheme) => scheme switch
    {
        SuiSignatureScheme.Ed25519 => 44,
        SuiSignatureScheme.EcdsaSecp256k1 => 54,
        SuiSignatureScheme.EcdsaSecp256r1 => 74,
        _ => throw new ArgumentException($"Unknown signature scheme: {scheme}")
    };

    /// <summary>
    /// Parse signature scheme from flag byte
    /// </summary>
    public static SuiSignatureScheme FromFlag(byte flag) => flag switch
    {
        0x00 => SuiSignatureScheme.Ed25519,
        0x01 => SuiSignatureScheme.EcdsaSecp256k1,
        0x02 => SuiSignatureScheme.EcdsaSecp256r1,
        _ => throw new ArgumentException($"Unknown signature scheme flag: 0x{flag:X2}")
    };

    /// <summary>
    /// Check if the scheme is supported by NSec.Cryptography
    /// </summary>
    public static bool IsSupportedByNSec(this SuiSignatureScheme scheme) => scheme switch
    {
        SuiSignatureScheme.Ed25519 => true,
        SuiSignatureScheme.EcdsaSecp256k1 => false, // Need additional ECDSA library
        SuiSignatureScheme.EcdsaSecp256r1 => false, // Need additional ECDSA library
        _ => false
    };
}