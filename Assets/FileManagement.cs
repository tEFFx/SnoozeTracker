using UnityEngine;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Text;

public class FileManagement : MonoBehaviour {
    public enum VGMCommands { StereoSet = 0x4F, PSGWrite = 0x50, WaitSamples = 0x61, Wait735 = 0x62, Wait882 = 0x63, EOF = 0x66 }

    public string fileFilter;
    public SongData data;
    public Instruments instruments;
    public InstrumentEditor insEditor;
    public SongPlayback playback;

    [System.Serializable]
    internal class SongFile {
        public string songName = "";
        public string artistName = "";
        public int patternLength;
        public List<int[]> lookupTable;
        public List<SongData.ColumnEntry> songData;
        public List<Instruments.InstrumentInstance> instruments;
    }
    
    public void SaveFile() {
        playback.Stop ( );

        SaveFileDialog sfd = new SaveFileDialog ( );
        sfd.Filter = fileFilter;

        if(sfd.ShowDialog() == DialogResult.OK ) {
            SongFile song = new SongFile ( );
            song.patternLength = data.patternLength;
            song.lookupTable = data.lookupTable;
            song.songData = data.songData;
            song.instruments = instruments.presets;

            song.songName = SongData.songName;
            song.artistName = SongData.artistName;

            IFormatter formatter = new BinaryFormatter ();
            Stream fs = sfd.OpenFile ( );
            formatter.Serialize ( fs, song );
            fs.Close ( );
        }
    }

    public void OpenFile() {
        playback.Stop ( );
        data.currentPattern = 0;

        OpenFileDialog ofd = new OpenFileDialog ( );
        ofd.Filter = fileFilter;

        if(ofd.ShowDialog() == DialogResult.OK ) {
            IFormatter formatter = new BinaryFormatter ( );
            Stream fs = ofd.OpenFile ( );

            SongFile open = (SongFile)formatter.Deserialize ( fs );
            data.SetPatternLength ( open.patternLength );
            data.lookupTable = open.lookupTable;
            data.songData = open.songData;

            SongData.songName = open.songName ?? "";
            SongData.artistName = open.artistName ?? "";

            instruments.presets = open.instruments;
            fs.Close ( );

            insEditor.UpdateAttributes ( );
        }
    }

    public void SaveVGM()
    {
        playback.Stop();
        playback.loop = false;
        playback.psg.audioSource.enabled = false;
        data.currentPattern = 0;

        SaveFileDialog sfd = new SaveFileDialog();
        sfd.Filter = "VGM-file (*.vgm)|*.vgm";

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            BinaryWriter bw = new BinaryWriter(sfd.OpenFile());
            bw.Write(Encoding.ASCII.GetBytes("Vgm ")); //0x00
            //wrote eof offset later when we know 0x04
            bw.Seek(0x08, SeekOrigin.Begin);
            bw.Write((uint)0x00000170); //0x08
            bw.Write((uint)playback.psg.chip.clock); //0x0C
            //gd3???? 0x14
            //write wait values later when we know 0x18
            //implement loop offset some rainy day 0x1C + 0x20
            bw.Seek(0x24, SeekOrigin.Begin);
            bw.Write((uint)playback.playbackRate); //0x24
            bw.Write((ushort)SN76489.NOISE_TAPPED); //0x28
            bw.Write((byte)SN76489.NOISE_SR_WIDTH); //0x2A
            bw.Write((byte)0x00);

            bw.Seek(0x34, SeekOrigin.Begin);
            bw.Write((uint)0x0C);

            bw.Seek(0x40, SeekOrigin.Begin);

            List<PSGWrapper.RegisterWrite> dataSamples = playback.psg.RecordRegisters();
            playback.Play();
            while (playback.isPlaying)
            {
                playback.psg.ManualClock();
            }
            playback.psg.RecordRegisters ( false );
            playback.psg.ResetChip ( );

            int loopWaitAmount = 0;
            int waitAmount = 0;
            int loopOffset = 0x40;
            int bytesWritten = 0;
            bool foundLoop = false;
            for (int i = 0; i < dataSamples.Count; i++)
            {
                if (dataSamples[i].wait > 0)
                {
                    switch (dataSamples[i].wait)
                    {
                        case 735:
                            bw.Write((byte)VGMCommands.Wait735);
                            bytesWritten++;
                            break;

                        case 882:
                            bw.Write((byte)VGMCommands.Wait882);
                            bytesWritten++;
                            break;

                        default:
                            int totalWait = dataSamples[i].wait;
                            do
                            {
                                bw.Write((byte)VGMCommands.WaitSamples);
                                bw.Write((ushort)System.Math.Min(65535, totalWait));
                                totalWait -= 65535;
                                bytesWritten += 3;
                            } while (totalWait > 65535);
                            break;
                    }

                    if(dataSamples[i].pattern >= playback.patternLoop)
                        loopWaitAmount += dataSamples[i].wait;
                    waitAmount += dataSamples [ i ].wait;
                }

                bw.Write ( ( byte ) dataSamples[i].command);
                bytesWritten++;
                if ( dataSamples [ i ].command != VGMCommands.EOF ) {
                    bw.Write ( ( byte ) dataSamples [ i ].data );
                    bytesWritten++;
                }

                if ( dataSamples [ i ].pattern == playback.patternLoop && i == 0 )
                    foundLoop = true;
                if ( !foundLoop && dataSamples [ i ].pattern == playback.patternLoop ) {
                    loopOffset += bytesWritten;
                    foundLoop = true;
                }
            }

            //bw.Write((byte)0x66);

            int gd3Offset = (int)bw.BaseStream.Position - 0x14;
            bw.Write(Encoding.ASCII.GetBytes("Gd3 "));
            bw.Write((uint)0x00010000);
            int sizeOffset = (int)bw.BaseStream.Position;
            bw.Write((uint)0);
            bw.Write(Encoding.Unicode.GetBytes(SongData.songName + "\0")); //track name
            bw.Write(Encoding.Unicode.GetBytes("\0"));
            bw.Write(Encoding.Unicode.GetBytes("\0")); //game name
            bw.Write(Encoding.Unicode.GetBytes("\0"));
            bw.Write(Encoding.Unicode.GetBytes( "Sega Game Gear\0" ) ); //system name
            bw.Write(Encoding.Unicode.GetBytes("\0"));
            bw.Write(Encoding.Unicode.GetBytes(SongData.artistName + "\0")); //artist name
            bw.Write(Encoding.Unicode.GetBytes("\0"));
            bw.Write(Encoding.Unicode.GetBytes( System.DateTime.Today.ToString ( @"yyyy\/MM\/dd" ) + "\0" )); //release date
            bw.Write(Encoding.Unicode.GetBytes("Unity Chiptune Tracker by tEFFx\0")); //converter
            bw.Write(Encoding.Unicode.GetBytes("\0")); //notes

            int eofOffset = (int)bw.BaseStream.Position - 0x04;

            bw.Seek(sizeOffset, SeekOrigin.Begin);
            bw.Write((uint)(eofOffset - sizeOffset));

            bw.Seek(0x04, SeekOrigin.Begin);
            bw.Write((uint)(eofOffset));

            bw.Seek(0x14, SeekOrigin.Begin);
            bw.Write((uint)(gd3Offset));

            bw.Seek(0x18, SeekOrigin.Begin);
            bw.Write((uint)waitAmount);

            bw.Write ( ( uint ) (loopOffset - 0x1C) );
            bw.Write ( ( uint ) (loopWaitAmount) );

            bw.Close();
        }

        playback.loop = true;
        playback.psg.audioSource.enabled = true;
        playback.psg.enabled = false;
        playback.psg.enabled = true;
        playback.Stop();
    }

    public bool LoadSample(ref int[] samples, ref int sampleRate) {
        OpenFileDialog ofd = new OpenFileDialog ( );
        ofd.Filter = "Wave-file (*.wav)|*.wav";

        if ( ofd.ShowDialog ( ) == DialogResult.OK ) {
            BinaryReader br = new BinaryReader ( ofd.OpenFile ( ) );

            WaveReader wav = new WaveReader ( br );

            if ( wav.loaded ) {
                sampleRate = wav.sampleRate;

                samples = new int [ wav.samples.Length ];
                for ( int i = 0 ; i < samples.Length ; i++ ) {
                    samples [ i ] = Mathf.RoundToInt ( ( wav.samples [ i ] / 255f ) * Instruments.InstrumentInstance.LINEAR_STEPS );
                }
            }

            br.Close ( );

            return wav.loaded;
        }

        return false;
    }

    public static bool CompareHeader(BinaryReader reader, string str) {
        byte [ ] ascii = Encoding.ASCII.GetBytes ( str );
        byte [ ] header = reader.ReadBytes ( ascii.Length );

        for ( int i = 0 ; i < ascii.Length ; i++ ) {
            if ( ascii [ i ] != header [ i ] )
                return false;
        }

        return true;
    }
}
