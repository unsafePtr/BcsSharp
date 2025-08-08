using System;
using BcsSharp.Core;
using Nethermind.Int256;

namespace BcsSharp.Example
{
    // Example struct for BCS serialization
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public uint Age { get; set; }
        public bool IsActive { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== BCS Sharp Examples ===\n");

            // Example 1: Basic types
            BasicTypesExample();

            // Example 2: Vectors
            VectorExample();

            // Example 3: Options
            OptionExample();

            // Example 4: Enums
            EnumExample();

            // Example 5: Large integers (u128, u256)
            LargeIntegerExample();

            // Example 6: Structs
            StructExample();

            Console.WriteLine("\nAll examples completed successfully!");
        }

        static void BasicTypesExample()
        {
            Console.WriteLine("1. Basic Types Example:");
            
            // Serialize basic types
            var u32Value = 123456u;
            var stringValue = "Hello, BCS!";
            var boolValue = true;

            var u32Bytes = Bcs.U32.Serialize(u32Value);
            var stringBytes = Bcs.String.Serialize(stringValue);
            var boolBytes = Bcs.Bool.Serialize(boolValue);

            Console.WriteLine($"   u32({u32Value}) -> {Convert.ToHexString(u32Bytes).ToLower()}");
            Console.WriteLine($"   string(\"{stringValue}\") -> {Convert.ToHexString(stringBytes).ToLower()}");
            Console.WriteLine($"   bool({boolValue}) -> {Convert.ToHexString(boolBytes).ToLower()}");

            // Deserialize back
            var deserializedU32 = Bcs.U32.Parse(u32Bytes);
            var deserializedString = Bcs.String.Parse(stringBytes);
            var deserializedBool = Bcs.Bool.Parse(boolBytes);

            Console.WriteLine($"   Deserialized: u32={deserializedU32}, string=\"{deserializedString}\", bool={deserializedBool}");
            Console.WriteLine();
        }

        static void VectorExample()
        {
            Console.WriteLine("2. Vector Example:");
            
            var vectorType = Bcs.Vector(Bcs.U32);
            var numbers = new uint[] { 1, 2, 3, 4, 5 };

            var serialized = vectorType.Serialize(numbers);
            var deserialized = vectorType.Parse(serialized);

            Console.WriteLine($"   vector<u32>[{string.Join(", ", numbers)}] -> {Convert.ToHexString(serialized).ToLower()}");
            Console.WriteLine($"   Deserialized: [{string.Join(", ", deserialized)}]");
            Console.WriteLine();
        }

        static void OptionExample()
        {
            Console.WriteLine("3. Option Example:");
            
            // Manual Option demonstration since OptionType needs refinement
            var writer = new BcsWriter();
            
            // Some value (true + value)
            writer.Reset();
            writer.WriteBool(true).Write32(42u);
            var someSerialized = writer.ToBytes();
            
            var reader = new BcsReader(someSerialized);
            var hasValue = reader.ReadBool();
            var someDeserialized = hasValue ? reader.Read32() : (uint?)null;

            Console.WriteLine($"   option<u32>(some(42)) -> {Convert.ToHexString(someSerialized).ToLower()}");
            Console.WriteLine($"   Deserialized: {someDeserialized}");

            // None value (false only)
            writer.Reset();
            writer.WriteBool(false);
            var noneSerialized = writer.ToBytes();
            
            reader = new BcsReader(noneSerialized);
            hasValue = reader.ReadBool();
            var noneDeserialized = hasValue ? reader.Read32() : (uint?)null;

            Console.WriteLine($"   option<u32>(none) -> {Convert.ToHexString(noneSerialized).ToLower()}");
            Console.WriteLine($"   Deserialized: {noneDeserialized}");
            Console.WriteLine();
        }

        static void EnumExample()
        {
            Console.WriteLine("4. Enum Example:");
            
            var colorEnum = BcsEnum.Create("Color")
                .AddVariant("Red")
                .AddVariant("Green") 
                .AddVariant("Blue")
                .Build();

            var shapeEnum = BcsEnum.Create("Shape")
                .AddVariant("Circle", Bcs.U32)
                .AddVariant("Rectangle", Bcs.U32)
                .AddVariant("Point")
                .Build();

            // Simple enum
            var redVariant = colorEnum.CreateVariant("Red");
            var redSerialized = colorEnum.Serialize(redVariant);
            var redDeserialized = colorEnum.Parse(redSerialized);

            Console.WriteLine($"   Color::Red -> {Convert.ToHexString(redSerialized).ToLower()}");
            Console.WriteLine($"   Deserialized: {redDeserialized}");

            // Enum with data
            var circleVariant = shapeEnum.CreateVariant("Circle", 10u);
            var circleSerialized = shapeEnum.Serialize(circleVariant);
            var circleDeserialized = shapeEnum.Parse(circleSerialized);

            Console.WriteLine($"   Shape::Circle(10) -> {Convert.ToHexString(circleSerialized).ToLower()}");
            Console.WriteLine($"   Deserialized: {circleDeserialized}");
            Console.WriteLine();
        }

        static void LargeIntegerExample()
        {
            Console.WriteLine("5. Large Integer Example (u128, u256):");
            
            // u128 example
            var u128Value = new UInt128(0x123456789ABCDEF0, 0x0FEDCBA987654321);
            var u128Serialized = Bcs.U128.Serialize(u128Value);
            var u128Deserialized = Bcs.U128.Parse(u128Serialized);

            Console.WriteLine($"   u128({u128Value}) -> {Convert.ToHexString(u128Serialized).ToLower()}");
            Console.WriteLine($"   Deserialized: {u128Deserialized}");

            // u256 example
            var u256Value = UInt256.Parse("123456789012345678901234567890123456789012345678901234567890");
            var u256Serialized = Bcs.U256.Serialize(u256Value);
            var u256Deserialized = Bcs.U256.Parse(u256Serialized);

            Console.WriteLine($"   u256({u256Value}) -> {Convert.ToHexString(u256Serialized).ToLower()}");
            Console.WriteLine($"   Deserialized: {u256Deserialized}");

            // Maximum values
            var maxU128 = UInt128.MaxValue;
            var maxU128Serialized = Bcs.U128.Serialize(maxU128);
            Console.WriteLine($"   u128::MAX -> {Convert.ToHexString(maxU128Serialized).ToLower()}");

            var maxU256 = UInt256.MaxValue;
            var maxU256Serialized = Bcs.U256.Serialize(maxU256);
            Console.WriteLine($"   u256::MAX -> {Convert.ToHexString(maxU256Serialized).ToLower()}");
            Console.WriteLine();
        }

        static void StructExample()
        {
            Console.WriteLine("6. Struct Example (Manual):");
            
            // For this example, we'll manually serialize a Person struct
            var person = new Person 
            { 
                Name = "Alice", 
                Age = 30, 
                IsActive = true 
            };

            // Manual serialization (in a real implementation, you'd use a StructType)
            var writer = new BcsWriter();
            writer.WriteString(person.Name)
                  .Write32(person.Age)
                  .WriteBool(person.IsActive);

            var serialized = writer.ToBytes();
            Console.WriteLine($"   Person{{name: \"{person.Name}\", age: {person.Age}, is_active: {person.IsActive}}}");
            Console.WriteLine($"   -> {Convert.ToHexString(serialized).ToLower()}");

            // Manual deserialization
            var reader = new BcsReader(serialized);
            var deserializedName = reader.ReadString();
            var deserializedAge = reader.Read32();
            var deserializedIsActive = reader.ReadBool();

            Console.WriteLine($"   Deserialized: Person{{name: \"{deserializedName}\", age: {deserializedAge}, is_active: {deserializedIsActive}}}");
            Console.WriteLine();
        }
    }
}
