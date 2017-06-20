using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System;
using Ionic.Zlib;

public class FileManagement : MonoBehaviour {
    public enum VGMCommands { StereoSet = 0x4F, PSGWrite = 0x50, WaitSamples = 0x61, Wait735 = 0x62, Wait882 = 0x63, EOF = 0x66 }

    public bool fileOpen { get { return m_OpenFile != ""; } }
    public static bool fileModified;

    public string[] fileFilters;
    public string filterDescription;
    public SongData data;
    public Instruments instruments;
    public InstrumentEditor insEditor;
    public SongPlayback playback;
    public VirtualKeyboard keyboard;
    public PatternView patternView;
    public PatternMatrix patternMatrix;
    public VGMImport vgmImport;

    public Action onFileOpen;

    private Thread m_Thread;
    private bool m_OperationInProgress;
    private float m_Progress;
    private string m_OpenFile = "";

    private TinyFileDialogs.OpenFileDialog m_TuneOpen;
    private TinyFileDialogs.OpenFileDialog m_SampleOpen;
    private TinyFileDialogs.OpenFileDialog m_VgmImport;

    private TinyFileDialogs.SaveFileDialog m_TuneSave;
    private TinyFileDialogs.SaveFileDialog m_WavSave;
    private TinyFileDialogs.SaveFileDialog m_VgmSave;

    [System.Serializable]
    internal class SongFile : ISerializable {
        public SongFile() {

        }

        public SongFile(SerializationInfo info, StreamingContext context) {
            fileVersion = -1;
            success = true;
            foreach ( SerializationEntry e in info ) {
                switch ( e.Name ) {
                    case "version":
                        fileVersion = ( int ) e.Value;

                        if(fileVersion > CURRENT_VERSION ) {
                            TinyFileDialogs.MessageBox ( "Incompatible file version!",
                                "Trying to open file saved by a newer version!\n" +
                                "(File version: " + fileVersion + ")",
                                TinyFileDialogs.DialogType.OK, TinyFileDialogs.IconType.ERROR, true );
                            success = false;
                            return;
                        }
                        break;
                    case "songName": songName = ( string ) e.Value; break;
                    case "artistName": artistName = (string) e.Value; break;
                    case "patternLength": patternLength = ( int ) e.Value; break;
                    case "lookupTable": lookupTable = ( List<int [ ]> ) e.Value; break;
                    case "transposeTable": transposeTable = ( List<int [ ]> ) e.Value; break;
                    case "songData": songData = ( List<SongData.ColumnEntry> ) e.Value; break;
                    case "instruments":
                        if(fileVersion < CURRENT_VERSION) {
                            instrumentsLegacy = ( List<Instruments.InstrumentInstance> ) info.GetValue("instruments", typeof(List<Instruments.InstrumentInstance>));
                        } else {
                            instruments = ( Instruments.InstrumentInstance [ ] ) e.Value;
                        }
                        break;
                }
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue ( "version", fileVersion );
            info.AddValue ( "songName", songName );
            info.AddValue ( "artistName", artistName );
            info.AddValue ( "patternLength", patternLength );
            info.AddValue ( "lookupTable", lookupTable);
            info.AddValue ( "transposeTable", transposeTable );
            info.AddValue ( "songData", songData );
            info.AddValue ( "instruments", instruments );
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context) {
            if( fileVersion < CURRENT_VERSION && instrumentsLegacy != null ) {
                Debug.Log ( "Importing instruments from older file version" );
                instruments = instrumentsLegacy.ToArray ( );
            }
        }

        public static readonly int CURRENT_VERSION = 10;
        public int fileVersion = CURRENT_VERSION;
        public string songName = "";
        public string artistName = "";
        public int patternLength;
        public List<int[]> lookupTable;
        public List<int[]> transposeTable;
        public List<SongData.ColumnEntry> songData;
        public List<Instruments.InstrumentInstance> instrumentsLegacy;      //legacy in file version 10
        public Instruments.InstrumentInstance[] instruments;                //added in file version 10
        public bool success;
    }

    void Awake() {
        m_TuneOpen = new TinyFileDialogs.OpenFileDialog ( );
        m_TuneOpen.title = "Open tune";
        m_TuneOpen.filterPatterns = fileFilters;
        m_TuneOpen.description = filterDescription;
        m_TuneOpen.defaultPath = UnityEngine.Application.dataPath;

        m_SampleOpen = new TinyFileDialogs.OpenFileDialog ( );
        m_SampleOpen.title = "Load sample";
        m_SampleOpen.filterPatterns = new string [ ] { "*.wav" };
        m_SampleOpen.description = "Wave-file (8-bit sample depth)";
        m_SampleOpen.defaultPath = UnityEngine.Application.dataPath;

        m_TuneSave = new TinyFileDialogs.SaveFileDialog ( );
        m_TuneSave.title = "Save tune";
        m_TuneSave.filterPatterns = fileFilters;
        m_TuneOpen.description = filterDescription;
        m_TuneOpen.defaultPath = UnityEngine.Application.dataPath;

        m_WavSave = new TinyFileDialogs.SaveFileDialog ( );
        m_WavSave.title = "Save WAVE";
        m_WavSave.filterPatterns = new string [ ] { "*.wav" };
        m_WavSave.description = "WAVE-file(s)";
        m_WavSave.defaultPath = UnityEngine.Application.dataPath;

        m_VgmSave = new TinyFileDialogs.SaveFileDialog ( );
        m_VgmSave.title = "Save VGM";
        m_VgmSave.filterPatterns = new string [ ] { "*.vgz", "*.vgm" };
        m_VgmSave.description = "VGM-file(s)";
        m_VgmSave.defaultPath = UnityEngine.Application.dataPath;
        
        m_VgmImport = new TinyFileDialogs.OpenFileDialog();
        m_VgmImport.title = "Import VGM";
        m_VgmImport.filterPatterns = new string [ ] { "*.vgm" };
        m_VgmImport.description = "Uncompressed VGM-file(s)";
        m_VgmImport.defaultPath = UnityEngine.Application.dataPath;
    }

    void Start() {
        fileModified = false;
    }

    void OnGUI() {
        if ( m_OperationInProgress ) {
            Rect boxRect = new Rect ( UnityEngine.Screen.width - 256, UnityEngine.Screen.height - 32, 256, 32 );
            GUI.Box ( boxRect, m_Progress + "%" );
        }
    }
    
    public void SaveFile(bool saveAs = true) {
        playback.Stop ( );

        if(!saveAs && (m_OpenFile == "" || !File.Exists ( m_OpenFile ) ) )
            saveAs = true;

        if(!saveAs || m_TuneSave.ShowDialog() ) {
            SongFile song = new SongFile ( );
            song.patternLength = data.patternLength;
            song.lookupTable = data.lookupTable;
            song.transposeTable = data.transposeTable;
            song.songData = data.songData;

            int presetCount = instruments.presets.Length;
            song.instruments = new Instruments.InstrumentInstance [ presetCount ];
            Array.Copy ( instruments.presets, song.instruments, presetCount );

            song.songName = SongData.songName;
            song.artistName = SongData.artistName;

            IFormatter formatter = new BinaryFormatter ();
            Stream fs = !saveAs ? new FileStream(m_OpenFile, FileMode.Create) : m_TuneSave.OpenFile ( );
            formatter.Serialize ( fs, song );
            fs.Close ( );

            if(saveAs)
                m_OpenFile = m_TuneSave.filePath;

            fileModified = false;
        }
    }

    public void ImportVGM()
    {
        if (fileModified && !TinyFileDialogs.MessageBox("Importing VGM",
                "Are you sure? You will lose all unsaved progress.", TinyFileDialogs.DialogType.YESNO,
                TinyFileDialogs.IconType.WARNING, false))
            return;

        if (m_VgmImport.ShowDialog())
        {
            BinaryReader br = new BinaryReader ( m_VgmImport.OpenFile ( ) );
            vgmImport.ImportVGMFile(br);
        }
    }

    public void OpenFile() {
        if ( fileModified && !TinyFileDialogs.MessageBox ( "Opening tune", "Are you sure? You will lose all unsaved progress.", TinyFileDialogs.DialogType.YESNO, TinyFileDialogs.IconType.WARNING, false ) )
            return;

        playback.Stop ( );
        data.currentPattern = 0;

        if(m_TuneOpen.ShowDialog()) {
            IFormatter formatter = new BinaryFormatter ( );
            Stream fs = m_TuneOpen.OpenFile ( );

            SongFile open = (SongFile)formatter.Deserialize ( fs );
            fs.Close ( );

            if ( !open.success )
                return;

            data.SetPatternLength ( open.patternLength );
            data.lookupTable = open.lookupTable;
            if ( open.transposeTable != null && open.transposeTable.Count == open.lookupTable.Count ) {
                data.transposeTable = open.transposeTable;
            } else {
                Debug.Log ( "Transpose table too shourt!!" );
                data.transposeTable = new List<int [ ]> ( );
                for ( int i = 0 ; i < data.lookupTable.Count ; i++ ) {

                    int [ ] transposeEntry;

                    if ( open.transposeTable != null && i < open.transposeTable.Count )
                        transposeEntry = open.transposeTable [ i ];
                    else
                        transposeEntry = new int [ data.channels ];

                    data.transposeTable.Add ( transposeEntry );
                }
            }
            data.songData = open.songData;

            SongData.songName = open.songName ?? "";
            SongData.artistName = open.artistName ?? "";

            keyboard.currentInstrument = 0;

            int presetCount = open.instruments.Length;
            Array.Resize ( ref instruments.presets, presetCount );
            Array.Copy ( open.instruments, instruments.presets, presetCount );
            
            m_OpenFile = m_TuneOpen.filePath;
            fileModified = false;

            if ( onFileOpen != null )
                onFileOpen ( );

            patternMatrix.UpdateMatrix();
            patternView.UpdatePatternData();
            insEditor.UpdateInstruments();
            insEditor.UpdateInstrumentInfo();
            insEditor.SetSelectedInstrument(0);
            playback.playbackSpeed = 6;
        }
    }

    public void SaveWAV()
    {
        if ( m_OperationInProgress )
            return;

        if (m_WavSave.ShowDialog())
        {
            StartCoroutine ( SaveWAVRoutine ( m_WavSave.OpenFile ( ), true ) );
        }
    }

    IEnumerator SaveWAVRoutine(Stream fileStream, bool normalize) {
        playback.Stop ( );
        playback.psg.audioSource.enabled = false;
        playback.follow = false;
        data.currentPattern = 0;

        m_OperationInProgress = true;

        BinaryWriter bw = new BinaryWriter ( fileStream );
        playback.Play ( true, true );

        List<double> samples = new List<double> ( );

        int c = 0;
        while ( playback.isPlaying ) {
            playback.psg.ManualClock ( );

            double left, right;
            playback.psg.chip.Render ( out left, out right );
            samples.Add ( left );
            samples.Add ( right );

            m_Progress = playback.songProgress * 100;

            c++;
            if(c > 44100) {
                c = 0;
                yield return null;
            }
        }

        m_Progress = 100;
        yield return null;

        WaveWriter.Write ( bw, samples.ToArray ( ), 2, ( uint ) AudioSettings.outputSampleRate, 16, normalize );

        m_OperationInProgress = false;

        playback.psg.audioSource.enabled = true;
        playback.psg.enabled = false;
        playback.psg.enabled = true;
        playback.follow = true;
        playback.Stop ( );

        m_Progress = 100;
        yield return null;
    }

    public void SaveMultiWAV() {
        if ( m_OperationInProgress )
            return;

        StartCoroutine ( SaveMultiWAVRoutine ( ) );
    }

    IEnumerator SaveMultiWAVRoutine() {
        m_OperationInProgress = true;
        string selectedPath = TinyFileDialogs.SelectFolderDialog ( "Save multiple WAVES", UnityEngine.Application.dataPath );

        if ( selectedPath == string.Empty || !Directory.Exists ( selectedPath ) ) {
            m_OperationInProgress = false;
            yield break;
        }

        for ( int i = 0 ; i < data.channels ; i++ ) {
            for ( int c = 0 ; c < data.channels ; c++ ) {
                playback.mute [ c ] = c != i;
            }    

            FileStream file = File.Create ( selectedPath + "/PSG_" + i + ".wav" );
            yield return StartCoroutine(SaveWAVRoutine ( file, false ));
            m_OperationInProgress = true;
        }

        for ( int c = 0 ; c < data.channels ; c++ ) {
            playback.mute [ c ] = false;
        }

        FileStream masterFile = File.Create ( selectedPath + "/PSG_ALL.wav" );
        yield return StartCoroutine(SaveWAVRoutine ( masterFile, true ));
        m_OperationInProgress = false;
    }


    public void SaveVGM()
    {
        if ( m_OperationInProgress )
            return;

        if ( m_VgmSave.ShowDialog())
        {
            StartCoroutine ( SaveVGMRoutine ( m_VgmSave ) );
        }
    }

    IEnumerator SaveVGMRoutine(TinyFileDialogs.SaveFileDialog sfd) {
        playback.Stop ( );
        playback.psg.audioSource.enabled = false;
        playback.follow = false;
        data.currentPattern = 0;
        m_OperationInProgress = true;

        bool isCompressed = sfd.filePath.Contains ( ".vgz" );
        Stream fileStream = isCompressed ? new MemoryStream ( ) : sfd.OpenFile ( );

        BinaryWriter bw = new BinaryWriter ( fileStream );
        bw.Write ( Encoding.ASCII.GetBytes ( "Vgm " ) ); //0x00
                                                         //wrote eof offset later when we know 0x04
        bw.Seek ( 0x08, SeekOrigin.Begin );
        bw.Write ( ( uint ) 0x00000170 ); //0x08
        bw.Write ( ( uint ) playback.psg.chip.clock ); //0x0C
                                                       //gd3???? 0x14
                                                       //write wait values later when we know 0x18
                                                       //implement loop offset some rainy day 0x1C + 0x20
        bw.Seek ( 0x24, SeekOrigin.Begin );
        bw.Write ( ( uint ) playback.playbackRate ); //0x24
        bw.Write ( ( ushort ) SN76489.NOISE_TAPPED ); //0x28
        bw.Write ( ( byte ) (SN76489.NOISE_SR_WIDTH + 1) ); //0x2A
        bw.Write ( ( byte ) 0x00 );

        bw.Seek ( 0x34, SeekOrigin.Begin );
        bw.Write ( ( uint ) 0x0C );

        bw.Seek ( 0x40, SeekOrigin.Begin );

        List<PSGWrapper.RegisterWrite> dataSamples = playback.psg.RecordRegisters ( );
        playback.Play ( false );
        int c = 0;

        while ( playback.isPlaying ) {
            playback.psg.ManualClock ( );

            m_Progress = playback.songProgress * 50;

            c++;
            if ( c > 44100 ) {
                c = 0;
                yield return null;
            }
        }
        playback.psg.RecordRegisters ( false );
        playback.psg.ResetChip ( );

        int loopWaitAmount = 0;
        int waitAmount = 0;
        int loopOffset = 0x40;
        int bytesWritten = 0;
        bool foundLoop = false;

        for ( int i = 0 ; i < dataSamples.Count ; i++ ) {
            if ( dataSamples [ i ].wait > 0 ) {
                switch ( dataSamples [ i ].wait ) {
                    case 735:
                        bw.Write ( ( byte ) VGMCommands.Wait735 );
                        bytesWritten++;
                        break;

                    case 882:
                        bw.Write ( ( byte ) VGMCommands.Wait882 );
                        bytesWritten++;
                        break;

                    default:
                        int totalWait = dataSamples [ i ].wait;
                        do {
                            bw.Write ( ( byte ) VGMCommands.WaitSamples );
                            bw.Write ( ( ushort ) System.Math.Min ( 65535, totalWait ) );
                            totalWait -= 65535;
                            bytesWritten += 3;
                        } while ( totalWait > 65535 );
                        break;
                }

                if ( dataSamples [ i ].pattern >= playback.patternLoop )
                    loopWaitAmount += dataSamples [ i ].wait;
                waitAmount += dataSamples [ i ].wait;

                c++;
                if(c > 44100 ) {
                    c = 0;
                    yield return null;
                }

                m_Progress = 50 + ( ( float ) i / dataSamples.Count ) * 50;
            }

            bw.Write ( ( byte ) dataSamples [ i ].command );
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

        int gd3Offset = ( int ) bw.BaseStream.Position - 0x14;
        bw.Write ( Encoding.ASCII.GetBytes ( "Gd3 " ) );
        bw.Write ( ( uint ) 0x00010000 );
        int sizeOffset = ( int ) bw.BaseStream.Position;
        bw.Write ( ( uint ) 0 );
        bw.Write ( Encoding.Unicode.GetBytes ( SongData.songName + "\0" ) ); //track name
        bw.Write ( Encoding.Unicode.GetBytes ( "\0" ) );
        bw.Write ( Encoding.Unicode.GetBytes ( "\0" ) ); //game name
        bw.Write ( Encoding.Unicode.GetBytes ( "\0" ) );
        bw.Write ( Encoding.Unicode.GetBytes ( "Sega Game Gear\0" ) ); //system name
        bw.Write ( Encoding.Unicode.GetBytes ( "\0" ) );
        bw.Write ( Encoding.Unicode.GetBytes ( SongData.artistName + "\0" ) ); //artist name
        bw.Write ( Encoding.Unicode.GetBytes ( "\0" ) );
        bw.Write ( Encoding.Unicode.GetBytes ( System.DateTime.Today.ToString ( @"yyyy\/MM\/dd" ) + "\0" ) ); //release date
        bw.Write ( Encoding.Unicode.GetBytes ( "SnoozeTracker\0" ) ); //converter
        bw.Write ( Encoding.Unicode.GetBytes ( "https://github.com/tEFFx/SnoozeTracker\0" ) ); //notes

        int eofOffset = ( int ) bw.BaseStream.Position - 0x04;

        bw.Seek ( sizeOffset, SeekOrigin.Begin );
        bw.Write ( ( uint ) ( eofOffset - sizeOffset ) );

        bw.Seek ( 0x04, SeekOrigin.Begin );
        bw.Write ( ( uint ) ( eofOffset ) );

        bw.Seek ( 0x14, SeekOrigin.Begin );
        bw.Write ( ( uint ) ( gd3Offset ) );

        bw.Seek ( 0x18, SeekOrigin.Begin );
        bw.Write ( ( uint ) waitAmount );

        if ( playback.patternLoop != 0 ) {
            bw.Write ( ( uint ) ( loopOffset - 0x1C ) );
            bw.Write ( ( uint ) ( loopWaitAmount ) );
        }

        if ( isCompressed ) {
            Stream fs = sfd.OpenFile ( );
            GZipStream gzipStream = new GZipStream ( fs, CompressionMode.Compress, CompressionLevel.BestCompression, true );
            byte [ ] buffer = new byte [ fileStream.Length ];
            fileStream.Position = 0;
            fileStream.Read ( buffer, 0, ( int ) fileStream.Length );
            gzipStream.Write ( buffer, 0, ( int ) fileStream.Length );
            gzipStream.Close ( );
        }

        bw.Close ( );

        playback.psg.audioSource.enabled = true;
        playback.psg.enabled = false;
        playback.psg.enabled = true;
        playback.follow = true;
        m_OperationInProgress = false;
        playback.Stop ( );
    }

    public bool LoadSample(ref int[] samples, ref int sampleRate) {
        if ( m_SampleOpen.ShowDialog ( ) ) {
            BinaryReader br = new BinaryReader ( m_SampleOpen.OpenFile ( ) );

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
