using System.Diagnostics.CodeAnalysis;

namespace BcsSharp.Core.Helpers;

internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowInvalidOperationException(string message)
    {
        throw new InvalidOperationException(message);
    }

    [DoesNotReturn]
    public static void ThrowEndOfStreamException(long count)
    {
        throw new EndOfStreamException($"Not enough bytes to read {count} bytes");
    }

    [DoesNotReturn]
    public static void ThrowContainerDepthExceeded()
    {
        throw new InvalidOperationException($"Structs and enums are nested deeper than the container depth limit (at most {BcsSerializer.MaxContainerDepth}).");
    }

    [DoesNotReturn]
    public static void ThrowSequenceTooLong(uint length)
    {
        throw new InvalidOperationException($"Sequence length {length} exceeds the BCS maximum of {BcsSerializer.MaxSequenceLength}.");
    }

    [DoesNotReturn]
    public static void ThrowRemainingBytes(int count)
    {
        throw new InvalidOperationException($"{count} bytes remain after the BCS value; the input must hold exactly one value.");
    }
}
