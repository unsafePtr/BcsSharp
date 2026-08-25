using BcsSharp.Core;

namespace BcsSharp.Tests;

/// <summary>
/// Direct tests for the pooling invariants of <see cref="ScratchBufferWriter"/>. The public
/// round-trip tests exercise it indirectly; these pin the rent/return contract itself, which
/// is what a buffered <see cref="BcsWriter"/> would have to rely on.
/// </summary>
public class ScratchBufferWriterTests
{
    [Fact]
    public void DoubleReturnDoesNotHandOneBufferToTwoLiveWriters()
    {
        var writer = ScratchBufferWriter.Rent();
        writer.Return();
        writer.Return();

        // If the second Return had put the same array back into the pool, these two live
        // writers could end up sharing it, and the second write would clobber the first.
        var first = ScratchBufferWriter.Rent();
        var second = ScratchBufferWriter.Rent();
        try
        {
            first.GetSpan(1)[0] = 0x01;
            first.Advance(1);
            second.GetSpan(1)[0] = 0x02;
            second.Advance(1);

            Assert.NotSame(first, second);
            Assert.Equal(0x01, first.WrittenSpan[0]);
            Assert.Equal(0x02, second.WrittenSpan[0]);
        }
        finally
        {
            first.Return();
            second.Return();
        }
    }

    [Fact]
    public void ReturnIsIdempotentAndLeavesTheSlotUsable()
    {
        var writer = ScratchBufferWriter.Rent();
        writer.GetSpan(4)[0] = 0xFF;
        writer.Advance(4);
        Assert.Equal(4, writer.WrittenCount);

        writer.Return();
        writer.Return();

        var reused = ScratchBufferWriter.Rent();
        try
        {
            Assert.Equal(0, reused.WrittenCount);
            Assert.Empty(reused.WrittenSpan.ToArray());
        }
        finally
        {
            reused.Return();
        }
    }

    [Fact]
    public void NestedRentGetsItsOwnInstanceSoTheOuterBufferSurvives()
    {
        var outer = ScratchBufferWriter.Rent();
        try
        {
            outer.GetSpan(1)[0] = 0xAA;
            outer.Advance(1);

            // A nested serialize (a map inside a map) rents while the outer one is live.
            var inner = ScratchBufferWriter.Rent();
            try
            {
                Assert.NotSame(outer, inner);
                inner.GetSpan(1)[0] = 0xBB;
                inner.Advance(1);
            }
            finally
            {
                inner.Return();
            }

            Assert.Equal(0xAA, outer.WrittenSpan[0]);
        }
        finally
        {
            outer.Return();
        }
    }

    [Fact]
    public void GrowPreservesWhatWasAlreadyWritten()
    {
        var writer = ScratchBufferWriter.Rent();
        try
        {
            writer.GetSpan(1)[0] = 0x7E;
            writer.Advance(1);

            // Ask for more than the 64 KB initial rental to force a Grow + copy.
            var span = writer.GetSpan(128 * 1024);
            span[0] = 0x7F;
            writer.Advance(1);

            Assert.Equal(0x7E, writer.WrittenSpan[0]);
            Assert.Equal(0x7F, writer.WrittenSpan[1]);
        }
        finally
        {
            writer.Return();
        }
    }
}
