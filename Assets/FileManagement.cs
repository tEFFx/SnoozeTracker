using UnityEngine;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;

public class FileManagement : MonoBehaviour {
    public string fileFilter;
    public SongData data;
    public Instruments instruments;

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
        SaveFileDialog sfd = new SaveFileDialog ( );
        sfd.Filter = fileFilter;

        if(sfd.ShowDialog() == DialogResult.OK ) {
            SongFile song = new SongFile ( );
            song.patternLength = data.patternLength;
            song.lookupTable = data.lookupTable;
            song.songData = data.songData;
            song.instruments = instruments.presets;

            IFormatter formatter = new BinaryFormatter ( );
            Stream fs = sfd.OpenFile ( );
            formatter.Serialize ( fs, song );
            fs.Close ( );
        }
    }

    public void OpenFile() {
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
        }
    }
}
