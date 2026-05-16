using BcsSharp.Core;
using Nethermind.Int256;

namespace BcsSharp.Tests.RustParity;

/// <summary>
/// Live cross-implementation parity tests. Each test serializes a C# value and asserts
/// the bytes match what the Rust <c>bcs</c> crate produced for the equivalent value
/// (fetched from the Dockerized fixture container).
///
/// All tests are <c>[Fact(Explicit = true)]</c> so they don't run on the default PR CI
/// (which avoids the Docker dependency). A dedicated CI job runs them via
/// <c>--explicit on</c> to catch drift between the Rust source and the C# serializer.
/// </summary>
[Collection(nameof(RustBcsContainerCollection))]
public class RustParityTests(RustBcsContainer rust)
{
    [Fact(Explicit = true)]
    public async Task User_BytesMatchRustExactly()
    {
        // Same field values as the Rust crate's main.rs constructs.
        var user = new RustBcsCompatibilityTests.User
        {
            Id = 12345,
            Name = "Alice",
            Email = null,
            Balance = UInt256.MaxValue,
            IsVerified = true,
            Address = new RustBcsCompatibilityTests.Address
            {
                Street = "",
                City = "Plovdiv",
                State = null,
                Zip = "12345",
            },
        };

        var csharpBytes = BcsSerializer.Serialize(user);
        var rustBytes = await rust.ReadFixtureAsync("user.bcs", TestContext.Current.CancellationToken);

        Assert.Equal(rustBytes, csharpBytes);
    }
}
