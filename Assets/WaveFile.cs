using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

public static class WaveWriter
{
    public static void Write(BinaryWriter writer, double[] samples, ushort channels, uint sampleRate, ushort bitDepth, bool normalize)
    {
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write((uint)((samples.Length * bitDepth) / 8 + 36));
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write((uint)16);
        writer.Write((ushort)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write((sampleRate * channels * bitDepth) / (uint)8);
        writer.Write((ushort)((channels * bitDepth) / 8));
        writer.Write(bitDepth);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write((uint)((samples.Length * bitDepth) / 8));

        if(normalize)
            NormalizeSamples ( samples, 0.95f, channels );

        for (int i = 0; i < samples.Length; i++)
        {
            switch (bitDepth)
            {
                case 8:
                    writer.Write((byte)((samples[i] + 1) * 0.5f * byte.MaxValue));
                    break;

                case 16:
                    writer.Write((short)(samples[i] * short.MaxValue));
                    break;

                case 32:
                    writer.Write(samples[i]);
                    break;
            }
        }

        writer.Close();
    }

    public static void NormalizeSamples(double[] samples, double attn, ushort channels) {
        double [ ] dividers = new double [ channels ];
        for ( int i = 0 ; i < samples.Length ; i++ ) {
            int chn = i % channels;
            dividers [ chn ] = System.Math.Max ( dividers [ chn ], samples [ i ] );
        }

        for ( int i = 0 ; i < samples.Length ; i++ ) {
            int chn = i % channels;
            samples [ i ] = ( samples [ i ] / dividers [ chn ] ) * 0.99f;
            samples [ i ] *= attn;
        }
    }
}

public class WaveReader {
    public WaveReader(BinaryReader reader) {
        if ( !FileManagement.CompareHeader(reader, "RIFF") ) {
            Debug.LogWarning ( "RIFF header not found in wave file" );
            loaded = false;
            return;
        }

        uint chunkSize = reader.ReadUInt32 ( );

        if ( !FileManagement.CompareHeader ( reader, "WAVE" ) ) {
            Debug.LogWarning ( "WAVE header not found in wave file" );
            loaded = false;
            return;
        }

        if ( !FileManagement.CompareHeader ( reader, "fmt " ) ) {
            Debug.LogWarning ( "fmt header not found in wave file" );
            loaded = false;
            return;
        }

        reader.BaseStream.Position = 22;
        channels = reader.ReadUInt16 ( );
        sampleRate = (int)reader.ReadUInt32 ( );

        reader.BaseStream.Position = 34;
        bitDepth = reader.ReadUInt16 ( );

        if ( !FileManagement.CompareHeader ( reader, "data" ) ) {
            Debug.LogWarning ( "data header not found in wave file" );
            loaded = false;
            return;
        }

        uint dataSize = reader.ReadUInt32 ( );
        samples = new int [ dataSize / ( bitDepth / 8 ) ];

        Debug.Log ( bitDepth );
        for ( int i = 0 ; i < dataSize ; i += bitDepth / 8 ) {
            if ( reader.BaseStream.Position >= reader.BaseStream.Length )
                break;

            samples [ i ] = bitDepth == 8 ? reader.ReadByte ( ) : reader.ReadUInt16 ( );
        }

        loaded = true;
    }

    public int channels;
    public int sampleRate;
    public int bitDepth;
    public int[] samples;

    public bool loaded;
}
