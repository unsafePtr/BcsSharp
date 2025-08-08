using System.Diagnostics;

namespace Sui.Types;

/// <summary>
/// Interface for cryptographic signers in Sui
/// Abstracts the signature scheme implementation details
/// </summary>
public interface ISigner
{
    /// <summary>
    /// The Sui address associated with this signer
    /// </summary>
    SuiAddress Address { get; }
    
    /// <summary>
    /// The signature scheme used by this signer
    /// </summary>
    SuiSignatureScheme Scheme { get; }

    /// <summary>
    /// Sign raw data bytes
    /// </summary>
    /// <param name="data">Data to sign</param>
    /// <returns>Sui signature</returns>
    SuiSignature Sign(byte[] data);

    /// <summary>
    /// Sign a transaction digest with proper intent handling
    /// </summary>
    /// <param name="digest">Transaction digest to sign</param>
    /// <param name="scope">Intent scope (defaults to TransactionData)</param>
    /// <returns>Sui signature</returns>
    SuiSignature SignTransaction(TransactionDigest digest, IntentScope scope = IntentScope.TransactionData);

    /// <summary>
    /// Sign a personal message (off-chain signing)
    /// </summary>
    /// <param name="message">Message to sign</param>
    /// <returns>Sui signature</returns>
    SuiSignature SignPersonalMessage(byte[] message);

    /// <summary>
    /// Verify a signature against data using this signer's public key
    /// </summary>
    /// <param name="data">Original data</param>
    /// <param name="signature">Signature to verify</param>
    /// <returns>True if signature is valid</returns>
    bool Verify(byte[] data, SuiSignature signature);

    /// <summary>
    /// Export the public key bytes for this signer
    /// </summary>
    /// <returns>Public key bytes</returns>
    byte[] ExportPublicKey();
}

/// <summary>
/// Interface for signers that support key export (for wallet operations)
/// </summary>
public interface IKeyExportableSigner : ISigner
{
    /// <summary>
    /// Export the private key bytes (use with caution)
    /// </summary>
    /// <returns>Private key bytes</returns>
    byte[] ExportPrivateKey();
    
    /// <summary>
    /// Export the private key as hex string
    /// </summary>
    /// <returns>Private key hex string</returns>
    string ExportPrivateKeyHex();
    
    /// <summary>
    /// Export the private key as base64 string
    /// </summary>
    /// <returns>Private key base64 string</returns>
    string ExportPrivateKeyBase64();
}

/// <summary>
/// Base abstract class for disposable signers
/// </summary>
[DebuggerDisplay("{ToString()}")]
public abstract class DisposableSigner : IKeyExportableSigner, IDisposable
{
    private bool _disposed;
    
    /// <inheritdoc />
    public abstract SuiAddress Address { get; }
    
    /// <inheritdoc />
    public abstract SuiSignatureScheme Scheme { get; }

    /// <inheritdoc />
    public abstract SuiSignature Sign(byte[] data);

    /// <inheritdoc />
    public virtual SuiSignature SignTransaction(TransactionDigest digest, IntentScope scope = IntentScope.TransactionData)
    {
        var intentMessage = CreateIntentMessage(digest.Bytes, scope);
        return Sign(intentMessage);
    }

    /// <inheritdoc />
    public virtual SuiSignature SignPersonalMessage(byte[] message)
    {
        var intentMessage = CreateIntentMessage(message, IntentScope.PersonalMessage);
        return Sign(intentMessage);
    }

    /// <inheritdoc />
    public abstract bool Verify(byte[] data, SuiSignature signature);

    /// <inheritdoc />
    public abstract byte[] ExportPublicKey();

    /// <inheritdoc />
    public abstract byte[] ExportPrivateKey();

    /// <inheritdoc />
    public abstract string ExportPrivateKeyHex();

    /// <inheritdoc />
    public abstract string ExportPrivateKeyBase64();

    /// <summary>
    /// Create intent message for signing (as per Sui specification)
    /// </summary>
    protected static byte[] CreateIntentMessage(byte[] data, IntentScope scope)
    {
        // Intent: [scope, version, app_id]
        var intent = new byte[] { (byte)scope, 0, 0 }; // version=0, app_id=0 for Sui
        
        // Intent message: intent || data
        var intentMessage = new byte[intent.Length + data.Length];
        intent.CopyTo(intentMessage, 0);
        data.CopyTo(intentMessage, intent.Length);
        
        return intentMessage;
    }

    /// <summary>
    /// Throw if disposed
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (!_disposed)
        {
            DisposeCore();
            _disposed = true;
        }
    }

    /// <summary>
    /// Override in derived classes to dispose resources
    /// </summary>
    protected abstract void DisposeCore();
    
    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name}({Scheme}, address={Address})";
}