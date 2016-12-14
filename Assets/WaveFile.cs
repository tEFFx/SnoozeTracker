using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

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
