using BcsSharp.Benchmarks.Generated;
using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Resolvers;
using BenchmarkDotNet.Attributes;
using Nethermind.Int256;
using OneOf;
using OneOf.Types;
using System.Buffers;

namespace BcsSharp.Benchmarks;

[MemoryDiagnoser]
public class SourceGeneratedResolverVsStandard
{
    [BcsStruct]
    public class User
    {
        [BcsField(0)]
        public ulong Id { get; set; }
        [BcsField(1)]
        public string Name { get; set; } = string.Empty;
        [BcsField(2)]
        public string? Email { get; set; }
        [BcsField(3)]
        public UInt256 Balance { get; set; }  // U256 in Rust
        [BcsField(4)]
        public bool IsVerified { get; set; }
        [BcsField(5)]
        public OneOf<None, Address> Address { get; set; }
    }

    [BcsStruct]
    public class Address
    {
        [BcsField(0)]
        public string Street { get; set; } = string.Empty;
        [BcsField(1)]
        public string City { get; set; } = string.Empty;
        [BcsField(2)]
        public string? State { get; set; }
        [BcsField(3)]
        public string Zip { get; set; } = string.Empty;
    }

    public static User _user = new User
    {
        Id = 12345,
        Name = "Alice",
        Email = "alice@example.com",
        Balance = UInt256.MaxValue,
        IsVerified = true,
        Address = new Address
        {
            Street = "123 Main St",
            City = "Plovdiv",
            State = "BG",
            Zip = "12345"
        }
    };

    public static IFormatterResolver SourceGeneratedResolver = BcsSharp.Generated.BcsSourceGeneratorResolver.Instance;
    public static IFormatterResolver DefaultCompositeResolver = CompositeResolver.Default;


    [Benchmark(Baseline = true)]
    public byte[] BenchmarkSourceGenerator()
    {
        return BcsSerializer.Serialize(_user, SourceGeneratedResolver);
    }

    [Benchmark]
    public byte[] BenchmarkStandardResolver()
    {
        return BcsSerializer.Serialize(_user, DefaultCompositeResolver);
    }
}
