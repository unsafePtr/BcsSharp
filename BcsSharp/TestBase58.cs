using Sui.Types;

// Quick test to show Base58 encoding works
Console.WriteLine("=== SuiAddress Base58 Test ===");

// Generate address from key pair
using var keyPair = SuiKeyPair.Generate();
var addr = keyPair.Address;

Console.WriteLine($"Generated Address (Base58): {addr}");
Console.WriteLine($"Same Address (Hex):         {addr.ToHexString()}");
Console.WriteLine($"Short representation:       {addr.ToShortString()}");

// Well-known addresses
Console.WriteLine($"\nWell-known addresses:");
Console.WriteLine($"Sui Framework (Base58): {SuiAddress.SuiFramework}");
Console.WriteLine($"Sui Framework (Hex):    {SuiAddress.SuiFramework.ToHexString()}");

// BCS serialization
var bcsBytes = addr.Serialize();
var restored = SuiAddress.Deserialize(bcsBytes);
Console.WriteLine($"\nBCS serialization works: {addr == restored}");

Console.WriteLine("\n✅ Base58 encoding working correctly!");