using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BcsSharp.Core.Helpers;

/// <summary>
/// SipHash-2-4, a keyed hash whose outputs cannot be predicted without the key, so colliding inputs cannot be precomputed.
/// </summary>
/// <remarks>Written from the specification at <see href="https://cr.yp.to/siphash/siphash-20120918.pdf"/>.</remarks>
internal readonly struct SipHash(ulong k0, ulong k1)
{
    public ulong Compute(ReadOnlySpan<byte> data)
    {
        var v0 = k0 ^ 0x736f6d6570736575UL;
        var v1 = k1 ^ 0x646f72616e646f6dUL;
        var v2 = k0 ^ 0x6c7967656e657261UL;
        var v3 = k1 ^ 0x7465646279746573UL;

        var blocks = data.Length & ~7;
        for (var i = 0; i < blocks; i += 8)
        {
            var m = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(i));
            v3 ^= m;
            Round(ref v0, ref v1, ref v2, ref v3);
            Round(ref v0, ref v1, ref v2, ref v3);
            v0 ^= m;
        }

        var last = (ulong)data.Length << 56;
        var tail = data.Slice(blocks);
        for (var i = 0; i < tail.Length; i++)
        {
            last |= (ulong)tail[i] << (8 * i);
        }

        v3 ^= last;
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);
        v0 ^= last;

        v2 ^= 0xff;
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);

        return v0 ^ v1 ^ v2 ^ v3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Round(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
    {
        v0 += v1;
        v1 = BitOperations.RotateLeft(v1, 13);
        v1 ^= v0;
        v0 = BitOperations.RotateLeft(v0, 32);
        v2 += v3;
        v3 = BitOperations.RotateLeft(v3, 16);
        v3 ^= v2;
        v0 += v3;
        v3 = BitOperations.RotateLeft(v3, 21);
        v3 ^= v0;
        v2 += v1;
        v1 = BitOperations.RotateLeft(v1, 17);
        v1 ^= v2;
        v2 = BitOperations.RotateLeft(v2, 32);
    }
}
