using UnityEngine;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Text;

public class FileManagement : MonoBehaviour {
    public string fileFilter;
    public SongData data;
    public Instruments instruments;
    public InstrumentEditor insEditor;
    public SongPlayback playback;

    [System.Serializable]
    internal class SongFile {
        public string songName;
        public string artistName;
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
            bw.Write((byte)SN76489.NOISE_SR_WIDTH);
            bw.Write((byte)0x04);

            bw.Seek(0x34, SeekOrigin.Begin);
            bw.Write((uint)0x0C);

            bw.Seek(0x40, SeekOrigin.Begin);

            List<PSGWrapper.RegisterWrite> dataSamples = playback.psg.RecordRegisters();
            playback.Play();
            while (playback.isPlaying)
            {
                playback.psg.ManualClock();
            }

            int waitAmount = 0;
            for (int i = 0; i < dataSamples.Count; i++)
            {
                if(dataSamples[i].wait > 0)
                {
                    switch (dataSamples[i].wait)
                    {
                        case 735:
                            bw.Write((byte)0x62);
                            break;

                        case 882:
                            bw.Write((byte)0x63);
                            break;

                        default:
                            int totalWait = dataSamples[i].wait;
                            do
                            {
                                bw.Write((byte)0x61);
                                bw.Write((ushort)System.Math.Min(65535, totalWait));
                                totalWait -= 65535;
                            } while (totalWait > 65535);
                            break;
                    }

                    waitAmount += dataSamples[i].wait;
                }

                bw.Write((byte)0x50);
                bw.Write((byte)dataSamples[i].data);
            }

            int eofOffset = (int)bw.BaseStream.Position;

            bw.Seek(0x04, SeekOrigin.Begin);
            bw.Write((uint)(eofOffset - 4));

            bw.Seek(0x18, SeekOrigin.Begin);
            bw.Write((uint)waitAmount);

            bw.Close();
        }

        playback.loop = true;
        playback.psg.audioSource.enabled = true;
        playback.psg.enabled = false;
        playback.psg.enabled = true;
        playback.psg.RecordRegisters(false);
        playback.Stop();
    }
}
