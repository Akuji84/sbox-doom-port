using System;
using System.IO;
using System.Text;

namespace MeltySynth
{
    internal sealed class SoundFontSampleData
    {
        private readonly int bitsPerSample;
        private readonly short[] samples;

        internal SoundFontSampleData(BinaryReader reader)
        {
            var chunkId = reader.ReadFourCC();
            if (chunkId != "LIST")
            {
                throw new Exception("The LIST chunk was not found.");
            }

            var end = (long)reader.ReadInt32();
            end += reader.BaseStream.Position;

            var listType = reader.ReadFourCC();
            if (listType != "sdta")
            {
                throw new Exception($"The type of the LIST chunk must be 'sdta', but was '{listType}'.");
            }

            while (reader.BaseStream.Position < end)
            {
                var id = reader.ReadFourCC();
                var size = reader.ReadInt32();

                switch (id)
                {
                    case "smpl":
                        bitsPerSample = 16;
                        var sampleBuffer = new short[size / 2];
                        var rawSampleBytes = reader.ReadBytes(size);
                        Buffer.BlockCopy(rawSampleBytes, 0, sampleBuffer, 0, size);
                        samples = sampleBuffer;
                        break;
                    case "sm24":
                        // 24 bit audio is not supported.
                        reader.BaseStream.Position += size;
                        break;
                    default:
                        throw new Exception($"The INFO list contains an unknown ID '{id}'.");
                }
            }

            if (samples == null)
            {
                throw new Exception("No valid sample data was found.");
            }

            var headerBytes = new byte[4];
            Buffer.BlockCopy(samples, 0, headerBytes, 0, 4);
            if (Encoding.ASCII.GetString(headerBytes) == "OggS")
            {
                throw new NotSupportedException("SoundFont3 is not yet supported.");
            }

            if (!BitConverter.IsLittleEndian)
            {
                // TODO: Insert the byte swapping code here.
                throw new NotSupportedException("Big endian architectures are not yet supported.");
            }
        }

        public int BitsPerSample => bitsPerSample;
        public short[] Samples => samples;
    }
}
