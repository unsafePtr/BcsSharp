# Sui.Types

Core Sui primitive types with BCS serialization and NSec.Cryptography integration.

## Overview

This library provides strongly-typed C# representations of fundamental Sui blockchain types:

- **SuiAddress** - 32-byte addresses derived from public keys
- **ObjectId** - 32-byte object identifiers  
- **ObjectRef** - Complete object references (ID + version + digest)
- **TransactionDigest** - 32-byte transaction identifiers
- **ObjectDigest** - 32-byte object content hashes
- **SuiKeyPair** - Ed25519 key pairs for signing
- **SuiSignature** - Sui-compatible signatures

## Key Features

✅ **Cryptographically Secure** - Uses NSec.Cryptography for all crypto operations  
✅ **Base58 Encoding** - Standard Sui address format with 0x prefix  
✅ **BCS Integration** - Native BCS serialization/deserialization support  
✅ **Type Safety** - Compile-time validation and proper type conversions  
✅ **Sui Compatible** - Follows Sui's exact specification for address/ID derivation  
✅ **Debugger Friendly** - DebuggerDisplay attributes show readable representations  
✅ **Performance** - Efficient implementations with minimal allocations

## Basic Usage

### Address Operations

```csharp
// Create from Base58 string with 0x prefix (standard Sui format)
var address = new SuiAddress("0xABC123...");  // Base58 encoding

// Create from hex string (backward compatibility)  
var hexAddress = new SuiAddress("0x1234567890abcdef...");

// Derive from public key (proper Sui derivation)
var keyPair = SuiKeyPair.Generate();
var address = keyPair.Address; // Derived using Blake2b-256

// System addresses
var suiFramework = SuiAddress.SuiFramework; // 0x2 (Base58 encoded)
var systemState = SuiAddress.SystemState;   // 0x5 (Base58 encoded)

// String representations
Console.WriteLine(address.ToString());     // Base58: "0xABC123..."
Console.WriteLine(address.ToHexString());  // Hex:    "0x1234abcd..."
Console.WriteLine(address.ToShortString()); // Short: "0xABC123"

// BCS Serialization
var bcsBytes = address.Serialize();
var restored = SuiAddress.Deserialize(bcsBytes);
```

### Object References

```csharp
// Create object reference
var objectRef = new ObjectRef(
    objectId: new ObjectId("0xabc..."), 
    version: 1,
    digest: new ObjectDigest("0xdef...")
);

// Version management
var nextVersion = objectRef.NextVersion(newDigest);
var isNewer = nextVersion.IsNewerThan(objectRef); // true

// BCS serialization
var bytes = objectRef.Serialize();
```

### Cryptographic Operations

```csharp
// Generate new key pair
using var keyPair = SuiKeyPair.Generate();
var address = keyPair.Address;

// Sign transaction
var digest = TransactionDigest.FromTransactionData(txData);
var signature = keyPair.SignTransaction(digest);

// Verify signature
var isValid = signature.VerifyTransaction(digest);
var signerAddress = signature.GetSignerAddress();

// Export keys
var privateKeyHex = keyPair.ExportPrivateKeyHex();
var publicKeyBytes = keyPair.ExportPublicKey();
```

### Type Conversions

```csharp
// Implicit string conversions  
SuiAddress addr = "0xABC123...";        // Base58 or hex
string addrStr = addr;                  // Base58: "0xABC123..."

// Address <-> ObjectId interoperability  
ObjectId objId = addr;
SuiAddress addr2 = objId;

// Multiple string formats
var base58 = addr.ToString();           // "0xABC123..." (Base58)
var hex = addr.ToHexString();          // "0x1234abcd..." (Hex)  
var short = addr.ToShortString();      // "0xABC123" (Short Base58)
```

## Cryptographic Details

### Address Derivation
Sui addresses are derived using **Blake2b-256**:
```
address = Blake2b-256(public_key_bytes || 0x00)
```

### Object ID Generation
Object IDs are derived from transaction digests:
```
object_id = Blake2b-256(transaction_digest || creation_index_u64_le)
```

### Signature Format
Sui signatures use this 97-byte format:
```
[signature_bytes(64) || public_key_bytes(32) || scheme_flag(1)]
```

### Intent-based Signing
Transaction signatures include an intent prefix:
```
signed_data = intent_bytes || transaction_data
intent = [scope(1) || version(1) || app_id(1)]
```

## Integration

### With BcsSharp.Core
All types implement BCS serialization:

```csharp
// Direct BCS operations
var writer = new BcsWriter();
address.Serialize();  // Uses internal BCS encoding

// Auto-struct serialization  
public class MyStruct
{
    public SuiAddress Owner { get; set; }
    public ObjectId Token { get; set; }
}

var bcsType = BcsStruct.Create<MyStruct>();
var data = bcsType.Serialize(myStruct);
```

### With NSec.Cryptography
Key operations use NSec for security:

```csharp
// Access underlying NSec types
PublicKey nsecPublicKey = keyPair.PublicKey;
var rawSignature = keyPair.Sign(data); // Raw Ed25519 signature

// Import existing keys
var imported = SuiKeyPair.FromSeed(seedBytes);
```

## Error Handling

```csharp
try
{
    var addr = new SuiAddress("invalid");
}
catch (ArgumentException ex)
{
    // Invalid format, length, etc.
}

try  
{
    using var keyPair = SuiKeyPair.FromSeed(shortSeed);
}
catch (ArgumentException ex)
{
    // Seed must be exactly 32 bytes
}
```

## Thread Safety

- All value types (`SuiAddress`, `ObjectId`, etc.) are thread-safe
- `SuiKeyPair` is **not** thread-safe and should not be shared across threads
- Always dispose `SuiKeyPair` when done (`using` statement recommended)

## Dependencies

- **.NET 9.0+**
- **BcsSharp.Core** - BCS serialization
- **NSec.Cryptography** - Cryptographic operations  
- **SimpleBase** - Base58 encoding/decoding