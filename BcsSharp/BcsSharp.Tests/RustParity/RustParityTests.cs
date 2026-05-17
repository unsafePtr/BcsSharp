using BcsSharp.Core;
using Nethermind.Int256;
using Attribute = BcsSharp.Tests.RustBcsCompatibilityTests.Attribute;

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
    public async Task User()
    {
        var user = SampleUser();

        var csharpBytes = BcsSerializer.Serialize(user);
        var rustBytes = await rust.ReadFixtureAsync("user.bcs", TestContext.Current.CancellationToken);

        Assert.Equal(rustBytes, csharpBytes);
    }

    [Fact(Explicit = true)]
    public async Task GameAsset()
    {
        var asset = SampleAsset();

        var csharpBytes = BcsSerializer.Serialize(asset);
        var rustBytes = await rust.ReadFixtureAsync("asset.bcs", TestContext.Current.CancellationToken);

        Assert.Equal(rustBytes, csharpBytes);
    }

    [Fact(Explicit = true)]
    public async Task Transaction()
    {
        var tx = SampleTransaction();

        var csharpBytes = BcsSerializer.Serialize(tx);
        var rustBytes = await rust.ReadFixtureAsync("transaction.bcs", TestContext.Current.CancellationToken);

        Assert.Equal(rustBytes, csharpBytes);
    }

    [Fact(Explicit = true)]
    public async Task MarketplaceItem()
    {
        var item = new RustBcsCompatibilityTests.MarketplaceItem
        {
            ItemId = "market_001",
            Seller = 12345,
            Asset = SampleAsset(),
            Price = 500,
            ListedAt = 1640995200,
            IsActive = true,
        };

        var csharpBytes = BcsSerializer.Serialize(item);
        var rustBytes = await rust.ReadFixtureAsync("marketplace.bcs", TestContext.Current.CancellationToken);

        Assert.Equal(rustBytes, csharpBytes);
    }

    [Fact(Explicit = true)]
    public async Task TupleExamples()
    {
        var tuples = new RustBcsCompatibilityTests.TupleExamples
        {
            SimplePair = ("hello", 42u),
            Triple = (123ul, true, "world"),
            NestedTuple = ("asset_ref", SampleAsset()),
            TupleWithArray = ("numbers", new List<uint> { 1, 2, 3, 4, 5 }),
            ComplexTuple = (SampleUser(), SampleTransaction(), false),
        };

        var csharpBytes = BcsSerializer.Serialize(tuples);
        var rustBytes = await rust.ReadFixtureAsync("tuples.bcs", TestContext.Current.CancellationToken);

        Assert.Equal(rustBytes, csharpBytes);
    }

    [Fact(Explicit = true)]
    public async Task MapExamples()
    {
        var maps = new RustBcsCompatibilityTests.MapExamples
        {
            StringToNumber = new Dictionary<string, uint>
            {
                ["health"] = 100,
                ["damage"] = 85,
                ["speed"] = 12,
            },
            NumberToString = new Dictionary<uint, string>
            {
                [1] = "common",
                [2] = "rare",
                [3] = "epic",
            },
            UserAttributes = new Dictionary<string, Attribute>
            {
                ["power"] = new Attribute { Name = "power", Value = 150 },
                ["defense"] = new Attribute { Name = "defense", Value = 75 },
            },
        };

        var csharpBytes = BcsSerializer.Serialize(maps);
        var rustBytes = await rust.ReadFixtureAsync("maps.bcs", TestContext.Current.CancellationToken);

        Assert.Equal(rustBytes, csharpBytes);
    }

    [Fact(Explicit = true)]
    public async Task LargeStringMap()
    {
        // 15 entries — verifies BCS key-byte sort order under realistic UTF-8 keys.
        var map = new Dictionary<string, uint>
        {
            ["zebra"] = 1000,
            ["alpha"] = 2000,
            ["beta"] = 3000,
            ["gamma"] = 4000,
            ["delta"] = 5000,
            ["epsilon"] = 6000,
            ["zeta"] = 7000,
            ["eta"] = 8000,
            ["theta"] = 9000,
            ["iota"] = 10000,
            ["kappa"] = 11000,
            ["lambda"] = 12000,
            ["mu"] = 13000,
            ["nu"] = 14000,
            ["xi"] = 15000,
        };

        var csharpBytes = BcsSerializer.Serialize(map);
        var rustBytes = await rust.ReadFixtureAsync("large_string_map.bcs", TestContext.Current.CancellationToken);

        Assert.Equal(rustBytes, csharpBytes);
    }

    // --- Sample data shared across tests (matches what Rust main.rs constructs) ---

    private static RustBcsCompatibilityTests.User SampleUser() => new()
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

    private static RustBcsCompatibilityTests.GameAsset SampleAsset() => new()
    {
        AssetId = "sword_001",
        AssetType = RustBcsCompatibilityTests.AssetType.Material,
        Level = 15,
        Attributes =
        [
            new Attribute { Name = "damage", Value = 85 },
            new Attribute { Name = "speed", Value = 12 },
        ],
    };

    private static RustBcsCompatibilityTests.Transaction SampleTransaction() => new()
    {
        TxId = "0x1234567890abcdef",
        FromUser = 12345,
        ToUser = 67890,
        Amount = 250,
        Timestamp = 1640995200,
        TxType = RustBcsCompatibilityTests.TransactionType.Transfer,
    };
}
