using System;

namespace ManagedDoom;

// SHA-256 implemented with managed arithmetic so game packages do not depend
// on System.Security.Cryptography, which is restricted by some s&box runtimes.
// Integrity/content identification only; this is not sender authentication.
internal static class SaveDataHash
{
    private static readonly uint[] K =
    {
        0x428a2f98,0x71374491,0xb5c0fbcf,0xe9b5dba5,0x3956c25b,0x59f111f1,0x923f82a4,0xab1c5ed5,
        0xd807aa98,0x12835b01,0x243185be,0x550c7dc3,0x72be5d74,0x80deb1fe,0x9bdc06a7,0xc19bf174,
        0xe49b69c1,0xefbe4786,0x0fc19dc6,0x240ca1cc,0x2de92c6f,0x4a7484aa,0x5cb0a9dc,0x76f988da,
        0x983e5152,0xa831c66d,0xb00327c8,0xbf597fc7,0xc6e00bf3,0xd5a79147,0x06ca6351,0x14292967,
        0x27b70a85,0x2e1b2138,0x4d2c6dfc,0x53380d13,0x650a7354,0x766a0abb,0x81c2c92e,0x92722c85,
        0xa2bfe8a1,0xa81a664b,0xc24b8b70,0xc76c51a3,0xd192e819,0xd6990624,0xf40e3585,0x106aa070,
        0x19a4c116,0x1e376c08,0x2748774c,0x34b0bcb5,0x391c0cb3,0x4ed8aa4a,0x5b9cca4f,0x682e6ff3,
        0x748f82ee,0x78a5636f,0x84c87814,0x8cc70208,0x90befffa,0xa4506ceb,0xbef9a3f7,0xc67178f2
    };

    private static uint Rotate(uint x, int n) => (x >> n) | (x << (32 - n));

    public static byte[] Compute(byte[] data)
    {
        uint[] h = { 0x6a09e667,0xbb67ae85,0x3c6ef372,0xa54ff53a,0x510e527f,0x9b05688c,0x1f83d9ab,0x5be0cd19 };
        var words = new uint[64];
        var block = new byte[64];
        var paddedLength = ((long)data.Length + 9 + 63) / 64 * 64;
        var bitLength = (ulong)data.Length * 8;
        unchecked
        {
            for (long offset = 0; offset < paddedLength; offset += 64)
            {
                for (var i = 0; i < 64; i++)
                {
                    var p = offset + i;
                    block[i] = p < data.Length ? data[(int)p] : p == data.Length ? (byte)0x80 : (byte)0;
                    if (p >= paddedLength - 8) block[i] = (byte)(bitLength >> (int)((paddedLength - 1 - p) * 8));
                }
                for (var i = 0; i < 16; i++)
                    words[i] = ((uint)block[i*4] << 24) | ((uint)block[i*4+1] << 16) | ((uint)block[i*4+2] << 8) | block[i*4+3];
                for (var i = 16; i < 64; i++)
                {
                    var x = words[i-15]; var y = words[i-2];
                    words[i] = words[i-16] + (Rotate(x,7) ^ Rotate(x,18) ^ (x>>3)) + words[i-7] + (Rotate(y,17) ^ Rotate(y,19) ^ (y>>10));
                }
                var a=h[0];var b=h[1];var c=h[2];var d=h[3];var e=h[4];var f=h[5];var g=h[6];var z=h[7];
                for (var i = 0; i < 64; i++)
                {
                    var t1=z+(Rotate(e,6)^Rotate(e,11)^Rotate(e,25))+((e&f)^(~e&g))+K[i]+words[i];
                    var t2=(Rotate(a,2)^Rotate(a,13)^Rotate(a,22))+((a&b)^(a&c)^(b&c));
                    z=g;g=f;f=e;e=d+t1;d=c;c=b;b=a;a=t1+t2;
                }
                h[0]+=a;h[1]+=b;h[2]+=c;h[3]+=d;h[4]+=e;h[5]+=f;h[6]+=g;h[7]+=z;
            }
        }
        var result = new byte[32];
        for (var i=0;i<8;i++)
            for(var j=0;j<4;j++) result[i*4+j]=(byte)(h[i]>>(24-j*8));
        return result;
    }
}
