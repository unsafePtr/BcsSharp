using Sui.Types;

namespace Sui.Examples;

/// <summary>
/// Examples demonstrating Sui signature and address functionality
/// Currently only Ed25519 is supported
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Sui.Types Examples ===\n");
        
        try
        {
            RunAddressExamples();
            RunSignatureExamples();
            RunDigestExamples();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("\n=== Examples Complete ===");
    }

    static void RunAddressExamples()
    {
        Console.WriteLine("=== Address Examples ===");
        
        // Generate Ed25519 key pair and address
        using var keyPair = SuiKeyPair.Generate(); // Only Ed25519 supported
        var address = keyPair.Address;
        
        Console.WriteLine($"Generated Address: {address}");
        Console.WriteLine($"Address (short):   {address.ToShortString()}");
        Console.WriteLine($"Address (hex):     {address.ToHexString()}");
        Console.WriteLine($"Scheme: {keyPair.Scheme}");
        
        // Demonstrate abstracted address derivation (works with any signature scheme)
        var addressFromSigner = SuiAddress.FromSigner(keyPair);
        Console.WriteLine($"Address from ISigner: {addressFromSigner}");
        Console.WriteLine($"Addresses match: {address == addressFromSigner}");
        Console.WriteLine($"NOTE: SuiAddress.FromSigner() works with any signature scheme!");
        
        // Well-known addresses
        Console.WriteLine($"\nWell-known addresses:");
        Console.WriteLine($"Zero Address:     {SuiAddress.Zero}");
        Console.WriteLine($"Sui Framework:    {SuiAddress.SuiFramework}");
        Console.WriteLine($"System State:     {SuiAddress.SystemState}");
        
        // Address parsing
        Console.WriteLine($"\nAddress parsing:");
        var hexAddr = new SuiAddress("0x0000000000000000000000000000000000000000000000000000000000000002");
        Console.WriteLine($"Parsed hex address: {hexAddr}");
        Console.WriteLine($"Matches SuiFramework: {hexAddr == SuiAddress.SuiFramework}");
        
        Console.WriteLine();
    }

    static void RunSignatureExamples()
    {
        Console.WriteLine("=== Ed25519 Signature Examples ===");
        
        // Key pair creation with scheme prefix export/import
        using var originalKeyPair = SuiKeyPair.Generate(); // Ed25519 only
        Console.WriteLine($"Original Key Pair: {originalKeyPair}");
        Console.WriteLine($"Address: {originalKeyPair.Address}");
        
        // Export with scheme prefix (format: [0x00][32-byte-key] for Ed25519)
        var base64WithScheme = originalKeyPair.ExportPrivateKeyBase64();
        Console.WriteLine($"Base64 with scheme prefix: {base64WithScheme[..20]}...");
        
        // Import - automatically detects Ed25519 from 0x00 prefix
        using var importedKeyPair = SuiKeyPair.FromBase64PrivateKey(base64WithScheme);
        Console.WriteLine($"Imported Key Pair: {importedKeyPair}");
        Console.WriteLine($"Addresses match: {originalKeyPair.Address == importedKeyPair.Address}");
        
        // Signing and verification
        var testData = "Hello, Sui blockchain!"u8.ToArray();
        var signature = importedKeyPair.Sign(testData);
        var isValid = importedKeyPair.Verify(testData, signature);
        
        Console.WriteLine($"Signature: {signature}");
        Console.WriteLine($"Verification: {isValid}");
        
        // Transaction signing example
        var txDigest = TransactionDigest.Random();
        var txSignature = importedKeyPair.SignTransaction(txDigest);
        var txValid = txSignature.VerifyTransaction(txDigest);
        
        Console.WriteLine($"Transaction signature: {txSignature}");
        Console.WriteLine($"Transaction verification: {txValid}");
        
        // Demonstrate unsupported schemes
        Console.WriteLine($"\nUnsupported schemes (Ed25519 only currently):");
        try
        {
            SuiKeyPair.Generate(SuiSignatureScheme.EcdsaSecp256k1);
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine($"ECDSA Secp256k1: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    static void RunDigestExamples()
    {
        Console.WriteLine("=== Digest Examples ===");
        
        // Transaction digests use Base58 encoding
        var txDigest = TransactionDigest.Random();
        Console.WriteLine($"Transaction Digest (Base58): {txDigest}");
        Console.WriteLine($"Transaction Digest (short):  {txDigest.ToShortString()}");
        
        // Object digests also use Base58 encoding  
        var objDigest = ObjectDigest.Random();
        Console.WriteLine($"Object Digest (Base58):      {objDigest}");
        Console.WriteLine($"Object Digest (short):       {objDigest.ToShortString()}");
        
        // Object IDs use hex encoding (like addresses)
        var objId = ObjectId.Random();
        Console.WriteLine($"Object ID (hex):             {objId}");
        Console.WriteLine($"Object ID (short):           {objId.ToShortString()}");
        
        // Object references combine ID, version, and digest
        var objRef = new ObjectRef(objId, 1, objDigest);
        Console.WriteLine($"Object Reference:            {objRef}");
        Console.WriteLine($"Object Reference (detailed): {objRef.ToDetailedString()}");
        
        // Round-trip serialization
        var digestString = txDigest.ToString();
        var parsedDigest = new TransactionDigest(digestString);
        Console.WriteLine($"Digest round-trip works: {txDigest.Equals(parsedDigest)}");
        
        Console.WriteLine();
    }
}
