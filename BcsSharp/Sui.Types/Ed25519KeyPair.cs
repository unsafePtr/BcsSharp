using System.Diagnostics;
using NSec.Cryptography;

namespace Sui.Types;

/// <summary>
/// Ed25519 implementation of SuiKeyPair using NSec.Cryptography
/// Provides secure Ed25519 key operations for Sui blockchain
/// </summary>
[DebuggerDisplay("{ToString()}")]
public sealed class Ed25519KeyPair : SuiKeyPair
{
    private readonly Key _privateKey;
    private readonly PublicKey _publicKey;

    /// <summary>
    /// The Ed25519 public key from NSec
    /// </summary>
    public PublicKey PublicKey => _publicKey;

    /// <summary>
    /// The signature scheme is always Ed25519
    /// </summary>
    public override SuiSignatureScheme Scheme => SuiSignatureScheme.Ed25519;

    /// <summary>
    /// Create an Ed25519 key pair from an existing private key
    /// </summary>
    private Ed25519KeyPair(Key privateKey) : base(DeriveAddressFromKey(privateKey))
    {
        _privateKey = privateKey;
        _publicKey = _privateKey.PublicKey;
    }

    /// <summary>
    /// Generate a new random Ed25519 key pair
    /// </summary>
    /// <returns>New Ed25519 key pair</returns>
    public static new Ed25519KeyPair Generate()
    {
        var algorithm = SignatureAlgorithm.Ed25519;
        var creationParameters = new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        };
        var privateKey = Key.Create(algorithm, creationParameters);
        return new Ed25519KeyPair(privateKey);
    }

    /// <summary>
    /// Create an Ed25519 key pair from a private key seed (32 bytes)
    /// </summary>
    /// <param name="seed">32-byte private key seed</param>
    /// <returns>Ed25519 key pair</returns>
    public static Ed25519KeyPair FromSeed(byte[] seed)
    {
        if (seed.Length != 32)
            throw new ArgumentException("Ed25519 seed must be exactly 32 bytes", nameof(seed));

        var algorithm = SignatureAlgorithm.Ed25519;
        var creationParameters = new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        };

        var privateKey = Key.Import(algorithm, seed, KeyBlobFormat.RawPrivateKey, creationParameters);
        return new Ed25519KeyPair(privateKey);
    }

    /// <summary>
    /// Create an Ed25519 key pair from raw private key bytes
    /// </summary>
    /// <param name="privateKeyBytes">32-byte private key</param>
    /// <returns>Ed25519 key pair</returns>
    internal static Ed25519KeyPair FromPrivateKeyBytes(byte[] privateKeyBytes)
    {
        return FromSeed(privateKeyBytes);
    }

    /// <summary>
    /// Create an Ed25519 key pair from a hex-encoded private key seed
    /// </summary>
    /// <param name="hexSeed">Hex encoded seed</param>
    /// <returns>Ed25519 key pair</returns>
    public static Ed25519KeyPair FromSeed(string hexSeed)
    {
        var seed = Convert.FromHexString(hexSeed);
        return FromSeed(seed);
    }

    /// <summary>
    /// Derive Sui address from Ed25519 key using abstracted method
    /// </summary>
    private static SuiAddress DeriveAddressFromKey(Key privateKey)
    {
        var publicKeyBytes = privateKey.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        return SuiAddress.FromPublicKeyBytes(publicKeyBytes, SuiSignatureScheme.Ed25519);
    }

    /// <inheritdoc />
    public override byte[] ExportPrivateKey()
    {
        ThrowIfDisposed();
        return _privateKey.Export(KeyBlobFormat.RawPrivateKey);
    }

    /// <inheritdoc />
    public override string ExportPrivateKeyHex()
    {
        return Convert.ToHexString(ExportPrivateKey()).ToLowerInvariant();
    }

    /// <inheritdoc />
    public override byte[] ExportPublicKey()
    {
        ThrowIfDisposed();
        return _publicKey.Export(KeyBlobFormat.RawPublicKey);
    }

    /// <inheritdoc />
    public override SuiSignature Sign(byte[] data)
    {
        ThrowIfDisposed();
        
        var algorithm = SignatureAlgorithm.Ed25519;
        var signatureBytes = algorithm.Sign(_privateKey, data);
        return new SuiSignature(signatureBytes, _publicKey);
    }

    /// <inheritdoc />
    public override bool Verify(byte[] data, SuiSignature signature)
    {
        ThrowIfDisposed();
        
        if (signature.Scheme != SuiSignatureScheme.Ed25519)
            return false;
        
        // Verify the signature was created by this key pair
        if (signature.NSecPublicKey == null || !_publicKey.Export(KeyBlobFormat.RawPublicKey).SequenceEqual(
                signature.NSecPublicKey.Export(KeyBlobFormat.RawPublicKey)))
            return false;
        
        var algorithm = SignatureAlgorithm.Ed25519;
        return algorithm.Verify(_publicKey, data, signature.SignatureBytes);
    }

    /// <summary>
    /// Verify a signature against this key pair's public key (raw signature bytes)
    /// </summary>
    /// <param name="data">Original data</param>
    /// <param name="signature">Raw signature bytes (64 bytes)</param>
    /// <returns>True if signature is valid</returns>
    public bool Verify(byte[] data, byte[] signature)
    {
        ThrowIfDisposed();
        
        if (signature.Length != 64)
            return false;
        
        var algorithm = SignatureAlgorithm.Ed25519;
        return algorithm.Verify(_publicKey, data, signature);
    }

    /// <summary>
    /// Sign raw data and return raw signature bytes
    /// </summary>
    /// <param name="data">Data to sign</param>
    /// <returns>Raw signature bytes (64 bytes)</returns>
    public byte[] SignRaw(byte[] data)
    {
        ThrowIfDisposed();
        
        var algorithm = SignatureAlgorithm.Ed25519;
        return algorithm.Sign(_privateKey, data);
    }

    /// <inheritdoc />
    protected override void DisposeCore()
    {
        _privateKey?.Dispose();
    }

    /// <inheritdoc />
    public override string ToString() => $"Ed25519KeyPair(address={Address})";
}