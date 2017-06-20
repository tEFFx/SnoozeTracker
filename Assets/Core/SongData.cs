using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime.Serialization;

public class SongData : MonoBehaviour {
    [Serializable]
    public class ColumnEntry : ISerializable
    {
        public static int s_PatternLength;

        public ColumnEntry(ColumnEntry origin)
        {
            data = new int[origin.data.GetLength(0), origin.data.GetLength(1)];
            Array.Copy(origin.data, data, origin.data.Length);

            m_DataEntries = origin.m_DataEntries;
            modified = origin.modified;
            m_ID = origin.m_ID;
        }

        public ColumnEntry(int _id)
        {
            data = new int[s_PatternLength, SONG_DATA_COUNT];
            m_DataEntries = SONG_DATA_COUNT;
            ResetRange ( 0, s_PatternLength );
            m_ID = _id;
        }

        public ColumnEntry(SerializationInfo info, StreamingContext context) {
            data = ( int [ , ] ) info.GetValue ( "data", typeof ( int [ , ] ) );
            m_DataEntries = data.GetLength ( 1 );
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue ( "data", data );
        }

        public int id { get { return m_ID; } }

        public int[,] data;
        public bool modified;
        private int m_DataEntries;
        private int m_ID;

        public void Resize() {
            Array temp = Array.CreateInstance ( data.GetType ( ).GetElementType ( ), s_PatternLength, data.GetLength ( 1 ) );
            int prevLen = data.GetLength(0);
            int len = Math.Min ( temp.Length, data.Length );
            Array.ConstrainedCopy ( data, 0, temp, 0, len );
            data = ( int [ , ] ) temp;

            if ( prevLen < data.GetLength(0) ) {
                ResetRange ( prevLen, data.GetLength(0) );
            }
        }

        public void SetID(int _newId) {
            m_ID = _newId;
        }

        private void ResetRange(int start, int end) {
            for ( int i = start ; i < end ; i++ ) {
                for ( int j = 0 ; j < m_DataEntries ; j++ ) {
                    data [ i, j ] = -1;
                }
            }
        }
    }

    public static readonly int SONG_DATA_COUNT = 5;

    public static string artistName = "";
    public static string songName = "";

    public int pageOffset { get { return channels * m_PatternLength * SONG_DATA_COUNT; } }
    public int numPatterns { get { return m_LookupTable.Count; } }
    public int patternLength { get { return m_PatternLength; } }
    public List<int[]> lookupTable { get { return m_LookupTable; } set { m_LookupTable = value; } }
    public List<int[]> transposeTable { get { return m_TransposeTable; } set { m_TransposeTable = value; } }
    public List<ColumnEntry> songData {
        get {
            return m_SongData;
        }
        set {
            m_SongData = value;
            for ( int i = 0 ; i < m_SongData.Count ; i++ ) {
                m_SongData [ i ].SetID ( i );
            }
        }
    }
    public ColumnEntry currentColumn { get { return GetPatternColumn ( currentPattern, patternView.position.channel ); } }

    public int this [ int i ] {
        get {
            int column, row, dataIndex;
            GetIndexOffset ( i, out column, out row, out dataIndex );
            if ( column < 0 )
                return -2;

            //Debug.Log("i=" +i + " col=" + column + " row=" + row);
            return m_SongData [ column ].data [ row, dataIndex ];
        }

        set {
            int column, row, dataIndex;
            GetIndexOffset ( i, out column, out row, out dataIndex );
            if ( column < 0 )
                return;

            m_SongData [ column ].data [ row, dataIndex ] = value;
            m_SongData [ column ].modified = true;

            FileManagement.fileModified = true;
        }
    }

    public PatternView patternView;
    public PatternMatrix patternMatrix;
    public int channels;
    public int currentPattern;

    private int m_CurrentLines;
    private int m_PatternLength;
    private List<int[]> m_LookupTable = new List<int[]>();
    private List<int[]> m_TransposeTable = new List<int[]>();
    private List<ColumnEntry> m_SongData = new List<ColumnEntry>();

    // Use this for initialization
    void Awake () {
        AddPatternLine();

        SetPatternLength ( 32 );
	}
	
	// Update is called once per frame
	void Update () {

    }

    public void OptimizeSong() {
        if ( !TinyFileDialogs.MessageBox ( "Optimizing song", "This will erase any unused patterns! Are you sure?", TinyFileDialogs.DialogType.YESNO, TinyFileDialogs.IconType.WARNING, false ) )
            return;

        Dictionary<int, int> sortedIndicies = new Dictionary<int, int> ( );
        List<ColumnEntry> sortedData = new List<ColumnEntry> ( );
        foreach(int[] row in m_LookupTable ) {
            for ( int i = 0 ; i < 4 ; i++ ) {
                int oldIndex = row [ i ];
                int newIndex = sortedData.Count;

                if (oldIndex >= 0 && !sortedIndicies.ContainsKey(oldIndex)) {
                    sortedData.Add ( m_SongData [ row [ i ] ] );
                    sortedIndicies.Add ( oldIndex, newIndex );
                }
            }
        }

        foreach ( int [ ] row in m_LookupTable ) {
            for ( int i = 0 ; i < 4 ; i++ ) {
                if ( sortedIndicies.ContainsKey ( row [ i ] ) )
                    row [ i ] = sortedIndicies [ row [ i ] ];
            }
        }

        m_SongData = sortedData;
        patternMatrix.UpdateMatrix();
        patternView.UpdatePatternData();
    }

    public void ZapSong() {
        if ( !TinyFileDialogs.MessageBox ( "Nuking song!", "This will erase the entire song. All unsaved progress will be lost!", TinyFileDialogs.DialogType.OKCANCEL, TinyFileDialogs.IconType.WARNING, false ) )
            return;

        patternView.playback.playbackSpeed = 6;
        UnityEngine.SceneManagement.SceneManager.LoadScene ( 0 );
    }

    public int FindLoopPoint() {
        for ( int i = 0 ; i < m_SongData.Count ; i++ ) {
            ColumnEntry col = m_SongData [ i ];
            for (int j = 0 ; j < col.data.GetLength(0) ; j++ ) {
                if ( col.data [ j, 3 ] == 0xB )
                    return col.data [ j, 4 ];
            }
        }

        return 0;
    }

    public void SetPatternLength(int len) {
        len = Math.Max ( len, 1 );
        ColumnEntry.s_PatternLength = len;

        for ( int i = 0 ; i < m_SongData.Count ; i++ ) {
            m_SongData [ i ].Resize ( );
        }

        m_PatternLength = len;
        patternView.UpdatePatternLength ( );
    }

    public ColumnEntry GetPatternColumn(int pattern, int col)
    {
        if ( m_LookupTable [ pattern ] [ col ] < 0 )
            return null;

        return m_SongData[m_LookupTable[pattern][col]];
    }

    public bool IsPatternValid(int channel) {
        return IsPatternValid(currentPattern, channel);
    }

    public bool IsPatternValid(int pattern, int channel)
    {
        if (pattern >= m_LookupTable.Count)
            return false;
        return m_LookupTable[pattern][channel] >= 0;
    }

    public void SetData(int channel, int row, int column, int data) {
        SetData ( currentPattern, channel, row, column, data );
    }

    public void SetData(int pattern, int channel, int row, int column, int data) {
        if (!IsPatternValid(pattern, channel))
            return;

        m_SongData [ m_LookupTable [ pattern ] [ channel ] ].data [ row, column ] = data;
    }

    public int GetData(int channel, int row, int column) {
        return GetData(currentPattern, channel, row, column);
    }

    public int GetData(int pattern, int channel, int row, int column) {
        if (!IsPatternValid(pattern, channel))
            return 0;

        return m_SongData[m_LookupTable[pattern][channel]].data[row, column];
    }

    public void IncrementLookup(int row, int col, int inc = 1)
    {
        int val = m_LookupTable[row][col] + inc;

        if (val < -1)
            return;

        AllocatePage(val);
        m_LookupTable[row][col] = val;
    }

    public int GetTransposeOffset(int chn) {
        return m_TransposeTable [ currentPattern ] [ chn ];
    }

    public void GetIndexOffset(int i, out int column, out int row, out int dataIndex)
    {
        int channel = (i / SONG_DATA_COUNT) % channels;
        row = i / (channels * SONG_DATA_COUNT);
        column = m_LookupTable[currentPattern][channel];
        dataIndex = i % SONG_DATA_COUNT;
    }

    public void AddPatternLine()
    {
        int [ ] table = new int [ channels ];
        if ( m_LookupTable.Count > 0 )
            currentPattern++;
        m_LookupTable.Insert ( currentPattern, table );
        m_TransposeTable.Insert ( currentPattern, new int [ channels ] );

        for (int j = 0; j < channels; j++)
        {
            int index = GetFirstUnallocatedIndex();
            AllocatePage(index);
            table[j] = index;
        }
    }

    public void MovePattern(int dir) {
        int targetIndex = currentPattern + dir;
        if ( targetIndex < 0 || targetIndex >= m_LookupTable.Count )
            return;

        int [ ] temp = m_LookupTable [ targetIndex ];
        m_LookupTable [ targetIndex ] = m_LookupTable [ currentPattern ];
        m_LookupTable [ currentPattern ] = temp;

        temp = m_TransposeTable [ targetIndex ];
        m_TransposeTable [ targetIndex ] = m_TransposeTable [ currentPattern ];
        m_TransposeTable [ currentPattern ] = temp;

        currentPattern += dir;
        patternMatrix.UpdateMatrix ( );
    }

    public void CopyPatternLine() {
        int [ ] table = new int [ channels ];
        Array.Copy ( m_LookupTable [ currentPattern ], table, m_LookupTable [ currentPattern ].Length );
        m_LookupTable.Add ( table );
        int[] lookup = new int[channels];
        Array.Copy(m_TransposeTable[currentPattern], lookup, m_TransposeTable[currentPattern].Length);
        m_TransposeTable.Add ( lookup );
    }

    public void DeletePatternLine() {
        if ( m_LookupTable.Count <= 1 )
            return;
        m_LookupTable.RemoveAt ( currentPattern );
        m_TransposeTable.RemoveAt(currentPattern);
        if ( currentPattern >= m_LookupTable.Count )
            currentPattern--;
    }

    public void AllocatePage(int index)
    {
        while(index >= m_SongData.Count)
        {
            m_SongData.Add(new ColumnEntry(m_SongData.Count));
        }
    }

    public int GetFirstUnallocatedIndex()
    {
        for (int i = 0; i < m_SongData.Count; i++)
        {
            if (!m_SongData[i].modified && !m_LookupTable.Any(x => x.Any(y => y == i)))
                return i;
        }

        return m_SongData.Count;
    }
}
