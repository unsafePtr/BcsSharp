using BcsSharp.Core;
using BcsSharp.Core.Formatters;
using BcsSharp.Core.Resolvers;
using OneOf;
using OneOf.Types;

namespace BcsSharp.Tests;

/// <summary>
/// OneOf&lt;None, T&gt; is no longer wired into <see cref="CompositeResolver.Default"/> — the
/// default Option path is now the C# 15 <c>union</c>. Downstream users who still rely on
/// the OneOf shape can plug it back in via <see cref="CustomFormatterResolver"/>. This
/// test proves that path still works end-to-end and that wire output matches the previous
/// default (1 byte tag + payload).
/// </summary>
public class OneOfManualRegistrationTests
{
    [Fact]
    public void RegisterOneOfFormatter_RoundTrips_AndProducesRustOptionWire()
    {
        IBcsFormatter<string> inner = StringFormatter.Instance;
        CustomFormatterResolver.Instance.Register<OneOf<None, string>>(new OneOfFormatter<string>(inner));
        BcsSerializer.ClearFormatterCache();

        try
        {
            // None: BCS Option<String> tag 0, no payload.
            OneOf<None, string> none = new None();
            var noneBytes = BcsSerializer.Serialize(none);
            Assert.Equal(new byte[] { 0x00 }, noneBytes);

            // Some("hi"): tag 1 + ULEB(2) + utf8.
            OneOf<None, string> some = "hi";
            var someBytes = BcsSerializer.Serialize(some);
            Assert.Equal(new byte[] { 0x01, 0x02, (byte)'h', (byte)'i' }, someBytes);

            var roundTripped = BcsSerializer.Deserialize<OneOf<None, string>>(someBytes);
            Assert.True(roundTripped.IsT1);
            Assert.Equal("hi", roundTripped.AsT1);
        }
        finally
        {
            CustomFormatterResolver.Instance.Unregister<OneOf<None, string>>();
            BcsSerializer.ClearFormatterCache();
        }
    }
}
