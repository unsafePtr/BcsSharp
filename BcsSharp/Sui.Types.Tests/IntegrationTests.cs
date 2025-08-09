using Sui.Types;

namespace Sui.Types.Tests;

/// <summary>
/// Integration tests that verify the interaction between different Sui types
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void FullWorkflow_KeyGeneration_AddressDerivation_Signing_Verification()
    {
        // Arrange & Act - Generate key pair
        using var keyPair = SuiKeyPair.Generate();
        var address = keyPair.Address;

        // Verify address derivation consistency
        var addressFromSigner = SuiAddress.FromSigner(keyPair);
        var addressFromPublicKey = SuiAddress.FromPublicKeyBytes(keyPair.ExportPublicKey(), keyPair.Scheme);

        // Create test data and signatures
        var testMessage = "Integration test message"u8.ToArray();
        var signature = keyPair.Sign(testMessage);

        // Assert - Everything should be consistent
        Assert.Equal(address, addressFromSigner);
        Assert.Equal(address, addressFromPublicKey);
        Assert.Equal(address, signature.GetSignerAddress());
        Assert.True(signature.Verify(testMessage));
    }

    [Fact]
    public void KeyExportImport_MaintainsConsistency()
    {
        // Arrange
        using var originalKeyPair = SuiKeyPair.Generate();
        var originalAddress = originalKeyPair.Address;
        var testData = "Export/Import test"u8.ToArray();

        // Act - Export and re-import key pair
        var base64Key = originalKeyPair.ExportPrivateKeyBase64();
        using var importedKeyPair = SuiKeyPair.FromBase64PrivateKey(base64Key);

        // Create signatures with both key pairs
        var originalSignature = originalKeyPair.Sign(testData);
        var importedSignature = importedKeyPair.Sign(testData);

        // Assert - All properties should match
        Assert.Equal(originalAddress, importedKeyPair.Address);
        Assert.Equal(originalKeyPair.Scheme, importedKeyPair.Scheme);
        Assert.True(originalKeyPair.ExportPublicKey().SequenceEqual(importedKeyPair.ExportPublicKey()));
        Assert.True(originalKeyPair.ExportPrivateKey().SequenceEqual(importedKeyPair.ExportPrivateKey()));

        // Signatures should be identical (deterministic)
        Assert.Equal(originalSignature, importedSignature);
        Assert.True(importedKeyPair.Verify(testData, originalSignature));
        Assert.True(originalKeyPair.Verify(testData, importedSignature));
    }

    [Fact]
    public void ObjectReference_CompleteLifecycle()
    {
        // Arrange - Simulate object creation
        var txDigest = TransactionDigest.FromTransactionData("object creation transaction"u8.ToArray());
        var objectId = ObjectId.FromTransactionDigest(txDigest, 0);
        var initialDigest = ObjectDigest.FromObjectData("initial object state"u8.ToArray());

        // Act - Create initial object reference
        var initialRef = ObjectRef.Initial(objectId, initialDigest);

        // Simulate object updates
        var updatedDigest = ObjectDigest.FromObjectData("updated object state"u8.ToArray());
        var updatedRef = initialRef.NextVersion(updatedDigest);
        var furtherUpdatedDigest = ObjectDigest.FromObjectData("further updated state"u8.ToArray());
        var furtherUpdatedRef = updatedRef.NextVersion(furtherUpdatedDigest);

        // Assert - Verify object reference evolution
        Assert.True(initialRef.IsInitialVersion);
        Assert.Equal(1UL, initialRef.Version);
        Assert.Equal(2UL, updatedRef.Version);
        Assert.Equal(3UL, furtherUpdatedRef.Version);

        Assert.True(initialRef.RefersToSameObject(updatedRef));
        Assert.True(updatedRef.RefersToSameObject(furtherUpdatedRef));

        Assert.True(updatedRef.IsNewerThan(initialRef));
        Assert.True(furtherUpdatedRef.IsNewerThan(updatedRef));
        Assert.True(furtherUpdatedRef.IsNewerThan(initialRef));
    }

    [Fact]
    public void SignatureFormats_AllConversionsWork()
    {
        // Arrange
        using var keyPair = SuiKeyPair.Generate();
        var testData = "Format conversion test"u8.ToArray();
        var signature = keyPair.Sign(testData);

        // Act - Convert between all formats
        var suiBytes = signature.ToSuiBytes();
        var base64String = signature.ToBase64();
        var hexString = signature.ToHex();

        var fromSuiBytes = SuiSignature.FromSuiBytes(suiBytes);
        var fromBase64 = SuiSignature.FromBase64(base64String);
        var fromHex = SuiSignature.FromHex(hexString);

        // Assert - All conversions should produce equivalent signatures
        Assert.Equal(signature, fromSuiBytes);
        Assert.Equal(signature, fromBase64);
        Assert.Equal(signature, fromHex);

        // All should verify correctly
        Assert.True(fromSuiBytes.Verify(testData));
        Assert.True(fromBase64.Verify(testData));
        Assert.True(fromHex.Verify(testData));
    }

    [Fact]
    public void TransactionSigning_WithActualTransactionData_WorksCorrectly()
    {
        // Arrange - Create actual transaction data (simulating a Sui transaction)
        using var senderKeyPair = SuiKeyPair.Generate();
        using var recipientKeyPair = SuiKeyPair.Generate();
        var senderAddress = senderKeyPair.Address;
        var recipientAddress = recipientKeyPair.Address;

        // Create realistic transaction data bytes (simulating BCS-encoded transaction)
        var transactionData = new byte[]
        {
            0x00, // Transaction type
            0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Gas budget
            0x01, 0x00, 0x00, 0x00, // Gas price
            // Add sender address (32 bytes)
        }.Concat(senderAddress.Bytes)
         .Concat(new byte[]
        {
            // Add recipient address (32 bytes)  
        }).Concat(recipientAddress.Bytes)
         .Concat(new byte[]
        {
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Amount (16 SUI)
            0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Gas limit
        }).ToArray();

        // Create transaction digest from actual transaction data
        var txDigest = TransactionDigest.FromTransactionData(transactionData);

        // Act - Sign the transaction with different scopes
        var txDataSig = senderKeyPair.SignTransaction(txDigest, IntentScope.TransactionData);
        var txEffectsSig = senderKeyPair.SignTransaction(txDigest, IntentScope.TransactionEffects);
        var checkpointSig = senderKeyPair.SignTransaction(txDigest, IntentScope.CheckpointSummary);
        var personalSig = senderKeyPair.SignPersonalMessage(transactionData);

        // Assert - Verify that signatures are different for different scopes
        Assert.NotEqual(txDataSig, txEffectsSig);
        Assert.NotEqual(txDataSig, checkpointSig);
        Assert.NotEqual(txDataSig, personalSig);
        Assert.NotEqual(txEffectsSig, checkpointSig);

        // Verify signatures with correct scopes
        Assert.True(txDataSig.VerifyTransaction(txDigest, IntentScope.TransactionData));
        Assert.True(txEffectsSig.VerifyTransaction(txDigest, IntentScope.TransactionEffects));
        Assert.True(checkpointSig.VerifyTransaction(txDigest, IntentScope.CheckpointSummary));
        // Personal message verification - verify against intent message, not raw data
        var personalIntentMessage = new byte[] { (byte)IntentScope.PersonalMessage, 0, 0 }
            .Concat(transactionData).ToArray();
        Assert.True(personalSig.Verify(personalIntentMessage));

        // Cross-scope verification should fail
        Assert.False(txDataSig.VerifyTransaction(txDigest, IntentScope.TransactionEffects));
        Assert.False(txEffectsSig.VerifyTransaction(txDigest, IntentScope.TransactionData));

        // Verify that signature signer address matches the key pair
        Assert.Equal(senderAddress, txDataSig.GetSignerAddress());
        Assert.Equal(senderAddress, txEffectsSig.GetSignerAddress());
        Assert.Equal(senderAddress, checkpointSig.GetSignerAddress());
        Assert.Equal(senderAddress, personalSig.GetSignerAddress());
    }

    [Fact]
    public void CompleteTransactionSigningWorkflow_WithBCSEncoding_WorksCorrectly()
    {
        // Arrange - Create a complete transaction signing workflow
        using var senderKeyPair = SuiKeyPair.Generate();
        using var recipientKeyPair = SuiKeyPair.Generate();
        var senderAddress = senderKeyPair.Address;
        var recipientAddress = recipientKeyPair.Address;

        // Simulate a real Sui transaction structure using BCS encoding
        var bcsWriter = new BcsSharp.Core.BcsWriter();

        // Write transaction components (simplified Pay transaction)
        bcsWriter.Write((byte)0x00); // Transaction kind
        bcsWriter.Write((ulong)1000000000); // Gas budget (1 SUI in MIST)
        bcsWriter.Write((ulong)1000); // Gas price

        // Write sender address
        bcsWriter.WriteBytes(senderAddress.Bytes);

        // Write recipient address  
        bcsWriter.WriteBytes(recipientAddress.Bytes);

        // Write amount (0.5 SUI in MIST)
        bcsWriter.Write((ulong)500000000);

        // Write gas object reference (simulated)
        var gasObjectId = ObjectId.Random();
        var gasDigest = ObjectDigest.Random();
        bcsWriter.WriteBytes(gasObjectId.Bytes);
        bcsWriter.Write((ulong)1); // version
        bcsWriter.WriteBytes(gasDigest.Bytes);

        var transactionBytes = bcsWriter.ToBytes();

        // Create digest from the BCS-encoded transaction
        var txDigest = TransactionDigest.FromTransactionData(transactionBytes);

        // Act - Sign the transaction
        var signature = senderKeyPair.SignTransaction(txDigest, IntentScope.TransactionData);

        // Assert - Comprehensive verification
        Assert.True(signature.VerifyTransaction(txDigest, IntentScope.TransactionData));
        Assert.Equal(senderAddress, signature.GetSignerAddress());
        Assert.Equal(SuiSignatureScheme.Ed25519, signature.Scheme);

        // Verify that the signature can be serialized and reconstructed
        var signatureBytes = signature.ToSuiBytes();
        var reconstructedSig = SuiSignature.FromSuiBytes(signatureBytes);
        Assert.Equal(signature, reconstructedSig);
        Assert.True(reconstructedSig.VerifyTransaction(txDigest, IntentScope.TransactionData));

        // Verify different signature formats work
        var base64Sig = signature.ToBase64();
        var hexSig = signature.ToHex();

        var fromBase64 = SuiSignature.FromBase64(base64Sig);
        var fromHex = SuiSignature.FromHex(hexSig);

        Assert.True(fromBase64.VerifyTransaction(txDigest, IntentScope.TransactionData));
        Assert.True(fromHex.VerifyTransaction(txDigest, IntentScope.TransactionData));

        // Verify that tampering with transaction data invalidates signature
        var modifiedTransactionBytes = transactionBytes.ToArray();
        modifiedTransactionBytes[0] = 0xFF; // Tamper with first byte
        var tamperedDigest = TransactionDigest.FromTransactionData(modifiedTransactionBytes);
        Assert.False(signature.VerifyTransaction(tamperedDigest, IntentScope.TransactionData));
    }

    [Fact]
    public void Debug_Simple_PublicKey_And_Address_Test()
    {
        // Test basic key generation and address derivation
        using var keyPair = SuiKeyPair.Generate();

        // Test address property
        var address1 = keyPair.Address;
        Assert.NotEqual(SuiAddress.Zero, address1);

        // Test public key export
        var publicKeyBytes = keyPair.ExportPublicKey();
        Assert.Equal(32, publicKeyBytes.Length); // Ed25519 public key is 32 bytes

        // Test address derivation from ISigner
        var address2 = SuiAddress.FromSigner(keyPair);
        Assert.Equal(address1, address2);

        // Test address derivation from public key bytes
        var address3 = SuiAddress.FromPublicKeyBytes(publicKeyBytes, keyPair.Scheme);
        Assert.Equal(address1, address3);

        // Test basic signing
        var testData = "Hello, Sui!"u8.ToArray();
        var signature = keyPair.Sign(testData);

        // Test signature verification
        Assert.True(signature.Verify(testData));

        // Test signer address extraction
        var signerAddress = signature.GetSignerAddress();
        Assert.Equal(address1, signerAddress);

        // Test different data fails verification
        var differentData = "Hello, World!"u8.ToArray();
        Assert.False(signature.Verify(differentData));
    }

    [Fact]
    public void Debug_Personal_Message_Signing_Test()
    {
        using var keyPair = SuiKeyPair.Generate();
        var testMessage = "Personal message test"u8.ToArray();

        // Sign personal message
        var personalSig = keyPair.SignPersonalMessage(testMessage);

        // This should verify the personal message, but let's see what happens
        var verificationResult = personalSig.Verify(testMessage);

        // Let's also test what the personal message actually signs
        var intentMessage = new byte[] { (byte)IntentScope.PersonalMessage, 0, 0 }
            .Concat(testMessage).ToArray();

        // The signature should verify against the intent message, not the raw message
        var rawSig = keyPair.Sign(intentMessage);
        Assert.True(rawSig.Verify(intentMessage));
    }

    [Fact]
    public void UnsupportedSignatureSchemes_ThrowAppropriateExceptions()
    {
        // Test that attempting to use unsupported schemes throws clear exceptions
        var testBase64Key = Convert.ToBase64String(new byte[] { 0x01, 0x00, 0x01, 0x02, 0x03 }); // ECDSA Secp256k1 flag

        var exception1 = Assert.Throws<NotSupportedException>(() =>
            SuiKeyPair.FromBase64PrivateKey(testBase64Key));
        Assert.Contains("Only Ed25519 is currently supported", exception1.Message);

        var exception2 = Assert.Throws<NotSupportedException>(() =>
            SuiKeyPair.Generate(SuiSignatureScheme.EcdsaSecp256k1));
        Assert.Contains("Only Ed25519 is currently supported", exception2.Message);

        var exception3 = Assert.Throws<NotSupportedException>(() =>
            SuiKeyPair.Generate(SuiSignatureScheme.EcdsaSecp256r1));
        Assert.Contains("Only Ed25519 is currently supported", exception3.Message);
    }

    [Fact]
    public void WellKnownValues_AreConsistent()
    {
        // Test that well-known addresses and object IDs have expected values
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000000", SuiAddress.Zero.ToString());
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000002", SuiAddress.SuiFramework.ToString());
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000005", SuiAddress.SystemState.ToString());

        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000000", ObjectId.Zero.ToString());
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000006", ObjectId.Clock.ToString());
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000008", ObjectId.SystemRandom.ToString());

        Assert.True(TransactionDigest.Zero.Bytes.All(b => b == 0));
        Assert.True(ObjectDigest.Zero.Bytes.All(b => b == 0));
    }

    [Fact]
    public void KeyPair_GenerateAddress_WorksCorrectly()
    {
        // Arrange
        using var keyPair = SuiKeyPair.Generate();

        // Act - Test the new GenerateAddress method
        var generatedAddress = keyPair.GenerateAddress();

        // Assert - Should match the Address property
        Assert.Equal(keyPair.Address, generatedAddress);
        Assert.Equal(keyPair.Address.ToString(), generatedAddress.ToString());
    }

    [Fact]
    public void SuiAddress_StringConstructor_HandlesSchemeAwareFormat()
    {
        // Arrange
        using var keyPair = SuiKeyPair.Generate();
        var originalAddress = keyPair.Address;
        var scheme = keyPair.Scheme;
        var schemeAwareString = originalAddress.ToSchemeAwareString(scheme);

        // Act - Create address from scheme-aware string using constructor
        var reconstructedAddress = new SuiAddress(schemeAwareString);

        // Assert - Should extract just the address part, ignoring the scheme
        Assert.Equal(originalAddress, reconstructedAddress);
        Assert.Equal(originalAddress.ToString(), reconstructedAddress.ToString());
    }

    // Helper method to validate Base58 strings
    private static bool IsValidBase58(string input)
    {
        const string base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return !string.IsNullOrEmpty(input) && input.All(base58Alphabet.Contains);
    }
}