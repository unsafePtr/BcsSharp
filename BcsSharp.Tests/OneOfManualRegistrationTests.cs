using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;
using BcsSharp.Core.Unions;
using BcsSharp.Tests.Extensibility;
using OneOf;
using BcsNone = BcsSharp.Core.Unions.None;
using OneOfNone = OneOf.Types.None;

namespace BcsSharp.Tests;

/// <summary>
/// Demonstrates the supported migration path for downstream consumers that still want to use OneOf&lt;None, T&gt; as their optional type.
/// The built-in resolver no longer wires <see cref="OneOfFormatter{T}"/> into <see cref="CompositeResolver.Default"/> — that slot is filled by <see cref="UnionResolver"/> for the C# 15 <c>union</c> default path.
/// Users plug OneOf back in by registering its formatters on <see cref="CustomFormatterResolver"/> at startup.
///
/// The two tests below also cross-verify that the OneOf wire output is byte-identical to the new <see cref="Option{T}"/> union output, so existing on-the-wire data remains compatible regardless of which optional type the user picks.
/// </summary>
public class OneOfManualRegistrationTests
{
    [Fact]
    public void Showcase_RegisterOneOfStringFormatter_RoundTripsAndMatchesUnionWire()
    {
        // 1) Register the OneOf<OneOfNone, string> formatter on a chain of our own.
        IBcsFormatter<string> stringFormatter = StringFormatter.Instance;
        var custom = new CustomFormatterResolver();
        custom.Register<OneOf<OneOfNone, string>>(new OneOfFormatter<string>(stringFormatter));
        var resolver = CompositeResolver.Create(custom);

        // 2) Use OneOf<OneOfNone, string> at the top level.
        OneOf<OneOfNone, string> oneOfNone = new OneOfNone();
        OneOf<OneOfNone, string> oneOfSome = "hi";

        var oneOfNoneBytes = BcsSerializer.Serialize(oneOfNone, resolver);
        var oneOfSomeBytes = BcsSerializer.Serialize(oneOfSome, resolver);

        // 3) Build the equivalent Option<string> values and serialize them via the
        //    default union path. Wire output must match exactly.
        Option<string> optionNone = BcsNone.Instance;
        Option<string> optionSome = "hi";
        var optionNoneBytes = BcsSerializer.Serialize(optionNone);
        var optionSomeBytes = BcsSerializer.Serialize(optionSome);

        Assert.Equal(optionNoneBytes, oneOfNoneBytes);
        Assert.Equal(optionSomeBytes, oneOfSomeBytes);

        // 4) Round-trip works.
        var roundTripped = BcsSerializer.Deserialize<OneOf<OneOfNone, string>>(oneOfSomeBytes, resolver);
        Assert.True(roundTripped.IsT1);
        Assert.Equal("hi", roundTripped.AsT1);
    }

    [Fact]
    public void Showcase_OneOfFieldInsideBcsStruct_RoundTripsAndMatchesUnionField()
    {
        // OneOf<None, T> typically appears as a field on a [BcsStruct]-annotated type.
        // Register the formatter for the field's exact closed generic — OneOf<OneOfNone, Address>.
        var addressFormatter = BcsSerializer.GetFormatter<Address>()!;
        var custom = new CustomFormatterResolver();
        custom.Register<OneOf<OneOfNone, Address>>(new OneOfFormatter<Address>(addressFormatter));
        var resolver = CompositeResolver.Create(custom);

        var withOneOf = new UserWithOneOf
        {
            Name = "alice",
            Address = new Address { City = "Plovdiv" },
        };
        var withUnion = new UserWithUnion
        {
            Name = "alice",
            Address = new Address { City = "Plovdiv" },
        };

        // The OneOf formatter is registered only on this chain, and UserWithOneOf reaches it
        // through a [BcsField] — so this passing is what proves field lookups follow the
        // chain that resolved the parent rather than the global default.
        var oneOfBytes = BcsSerializer.Serialize(withOneOf, resolver);
        var unionBytes = BcsSerializer.Serialize(withUnion);

        // Both struct shapes encode to the same bytes — the BCS wire format only cares
        // about the field type's discriminator + payload layout, not the C# wrapper.
        Assert.Equal(unionBytes, oneOfBytes);

        var back = BcsSerializer.Deserialize<UserWithOneOf>(oneOfBytes, resolver);
        Assert.Equal("alice", back.Name);
        Assert.True(back.Address.IsT1);
        Assert.Equal("Plovdiv", back.Address.AsT1.City);
    }

    // --- Fixtures -------------------------------------------------------------

    [BcsStruct]
    public sealed class Address
    {
        [BcsField(0)] public string City { get; set; } = "";
    }

    /// <summary>Legacy-style holder that keeps OneOf&lt;None, T&gt; as its optional shape.</summary>
    [BcsStruct]
    public sealed class UserWithOneOf
    {
        [BcsField(0)] public string Name { get; set; } = "";
        [BcsField(1)] public OneOf<OneOfNone, Address> Address { get; set; } = new OneOfNone();
    }

    /// <summary>Modern equivalent using the C# 15 union default path.</summary>
    [BcsStruct]
    public sealed class UserWithUnion
    {
        [BcsField(0)] public string Name { get; set; } = "";
        [BcsField(1)] public Option<Address> Address { get; set; } = BcsNone.Instance;
    }
}
