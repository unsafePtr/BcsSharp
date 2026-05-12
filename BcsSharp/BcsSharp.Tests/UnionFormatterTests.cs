using System.Runtime.CompilerServices;
using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Unions;

namespace BcsSharp.Tests;

/// <summary>
/// Round-trip and wire-format tests for C# 15 unions via <c>UnionFormatter</c>.
/// </summary>
public class UnionFormatterTests
{
    [Fact]
    public void Option_None_SerializesAsSingleZeroByte()
    {
        Option<string> opt = None.Instance;

        var bytes = BcsSerializer.Serialize(opt);

        // BCS Option<T>: tag 0 = None, no payload.
        Assert.Equal(new byte[] { 0x00 }, bytes);
    }

    [Fact]
    public void Option_Some_String_RoundTrips()
    {
        Option<string> opt = "hello";

        var bytes = BcsSerializer.Serialize(opt);
        var back = BcsSerializer.Deserialize<Option<string>>(bytes);

        // BCS Option<String>: tag 1 + ULEB-length-prefixed UTF-8.
        // "hello" is 5 ASCII bytes; ULEB(5) = 0x05.
        Assert.Equal(new byte[] { 0x01, 0x05, (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o' }, bytes);
        Assert.Equal("hello", ((IUnion)back).Value);
    }

    [Fact]
    public void Option_Inside_BcsStruct_RoundTrips()
    {
        var user = new User { Id = 7u, Nick = "alice", Address = new Address { City = "Plovdiv" } };

        var bytes = BcsSerializer.Serialize(user);
        var back = BcsSerializer.Deserialize<User>(bytes);

        Assert.Equal(user.Id, back.Id);
        Assert.Equal(user.Nick, back.Nick);
        Assert.Equal("Plovdiv", ((Address)((IUnion)back.Address).Value!).City);
    }

    [Fact]
    public void Option_Inside_BcsStruct_NoneVariant_RoundTrips()
    {
        var user = new User { Id = 7u, Nick = "alice", Address = None.Instance };

        var bytes = BcsSerializer.Serialize(user);
        var back = BcsSerializer.Deserialize<User>(bytes);

        Assert.IsType<None>(((IUnion)back.Address).Value);
    }

    [Fact]
    public void MultiVariant_Union_RoundTrips()
    {
        Command transfer = new Transfer(toAddress: "0xbeef", amount: 42);
        Command call = new MoveCall(moduleName: "coin", function: "split");

        var tBytes = BcsSerializer.Serialize(transfer);
        var cBytes = BcsSerializer.Serialize(call);

        // Variant indices are declaration order — MoveCall=0, Transfer=1.
        Assert.Equal(0x01, tBytes[0]);
        Assert.Equal(0x00, cBytes[0]);

        var tBack = BcsSerializer.Deserialize<Command>(tBytes);
        var cBack = BcsSerializer.Deserialize<Command>(cBytes);

        Assert.IsType<Transfer>(((IUnion)tBack).Value);
        Assert.IsType<MoveCall>(((IUnion)cBack).Value);
    }

    [Fact]
    public void Option_Construction_IsZeroAlloc_ForClassPayload()
    {
        var addr = new Address { City = "Sofia" };
        Option<Address> sink = default;

        // Warm up
        for (int i = 0; i < 1000; i++) sink = addr;
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100_000; i++)
        {
            sink = addr;
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        var perOp = (after - before) / 100_000.0;
        Assert.True(perOp < 1.0, $"Expected ~0 B/op for Option<Address> = addr, got {perOp} B/op");
    }

    [Fact]
    public void Option_None_Singleton_IsZeroAlloc()
    {
        Option<Address> sink = default;
        for (int i = 0; i < 1000; i++) sink = None.Instance;
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100_000; i++)
        {
            sink = None.Instance;
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        var perOp = (after - before) / 100_000.0;
        Assert.True(perOp < 1.0, $"Expected ~0 B/op for None.Instance assignment, got {perOp} B/op");
    }

    // --- Fixtures -------------------------------------------------------------

    [BcsStruct]
    public sealed class Address
    {
        [BcsField(0)] public string City { get; set; } = "";
    }

    [BcsStruct]
    public sealed class User
    {
        [BcsField(0)] public uint Id { get; set; }
        [BcsField(1)] public string Nick { get; set; } = "";
        [BcsField(2)] public Option<Address> Address { get; set; } = None.Instance;
    }

    [BcsStruct]
    public sealed class MoveCall
    {
        [BcsField(0)] public string ModuleName { get; set; } = "";
        [BcsField(1)] public string Function { get; set; } = "";

        public MoveCall() { }
        public MoveCall(string moduleName, string function) { ModuleName = moduleName; Function = function; }
    }

    [BcsStruct]
    public sealed class Transfer
    {
        [BcsField(0)] public string ToAddress { get; set; } = "";
        [BcsField(1)] public ulong Amount { get; set; }

        public Transfer() { }
        public Transfer(string toAddress, ulong amount) { ToAddress = toAddress; Amount = amount; }
    }

    public union Command(MoveCall, Transfer);
}
