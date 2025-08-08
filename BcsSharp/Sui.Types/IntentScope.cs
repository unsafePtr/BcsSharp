namespace Sui.Types;

/// <summary>
/// Intent scope for Sui transaction signing
/// Defines the context in which a signature is created
/// </summary>
public enum IntentScope : byte
{
    /// <summary>
    /// Transaction data signing (most common)
    /// </summary>
    TransactionData = 0,
    
    /// <summary>
    /// Transaction effects signing
    /// </summary>
    TransactionEffects = 1,
    
    /// <summary>
    /// Checkpoint summary signing
    /// </summary>
    CheckpointSummary = 2,
    
    /// <summary>
    /// Personal message signing (off-chain)
    /// </summary>
    PersonalMessage = 3
}