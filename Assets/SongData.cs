using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class SongData : MonoBehaviour {
    [System.Serializable]
    public class ColumnEntry
    {
        public ColumnEntry(int numRows, int numDataEntries)
        {
            data = new int[numRows, numDataEntries];
            m_DataEntries = numDataEntries;
            ResetRange ( 0, numRows );
        }

        public int[,] data;
        public bool modified;
        private int m_DataEntries;

        public void Resize(int numRows) {
            Array temp = Array.CreateInstance ( data.GetType ( ).GetElementType ( ), numRows, data.GetLength ( 1 ) );
            int prevLen = data.GetLength(0);
            int len = Math.Min ( temp.Length, data.Length );
            Array.ConstrainedCopy ( data, 0, temp, 0, len );
            data = ( int [ , ] ) temp;

            if ( prevLen < data.GetLength(0) ) {
                ResetRange ( prevLen, data.GetLength(0) );
            }
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

    public int pageOffset { get { return channels * m_PatternLength * SONG_DATA_COUNT; } }
    public int numPatterns { get { return m_LookupTable.Count; } }
    public int patternLength { get { return m_PatternLength; } }
    public List<int[]> lookupTable { get { return m_LookupTable; } }
    public ColumnEntry currentColumn { get { return GetCurrentLine ( currentPattern, patternView.selectedChannel ); } }


    public int this[int i]
    {
        get
        {
            int column, row, dataIndex;
            GetIndexOffset(i, out column, out row, out dataIndex);
            //Debug.Log("i=" +i + " col=" + column + " row=" + row);
            return m_SongData[column].data[row, dataIndex];
        }

        set
        {
            int column, row, dataIndex;
            GetIndexOffset(i, out column, out row, out dataIndex);
            m_SongData[column].data[row, dataIndex] = value;
            m_SongData[column].modified = true;
        }
    }

    public PatternView patternView;
    public int channels;
    public int currentPattern;

    private int m_CurrentLines;
    private int m_PatternLength;
    private List<int[]> m_LookupTable = new List<int[]>();
    private List<ColumnEntry> m_SongData = new List<ColumnEntry>();

    // Use this for initialization
    void Awake () {
        AddPatternLine();

        SetPatternLength ( 32 );
	}
	
	// Update is called once per frame
	void Update () {
        if ( Input.GetKeyDown ( KeyCode.Delete ) ) {
            currentColumn.data [patternView.currentLine, patternView.selectedAttribute ] = -1;
            if ( patternView.selectedAttribute == 0 )
                currentColumn.data [ patternView.currentLine, 1 ] = -1;
            patternView.MoveLine ( 1 );
        }
    }

    public void SetPatternLength(int len) {
        len = Math.Max ( len, 1 );

        for ( int i = 0 ; i < m_SongData.Count ; i++ ) {
            m_SongData [ i ].Resize ( len );
        }

        m_PatternLength = len;
    }

    public ColumnEntry GetCurrentLine(int pattern, int col)
    {
        return m_SongData[m_LookupTable[pattern][col]];
    }

    public void IncrementLookup(int row, int col, int inc = 1)
    {
        int val = m_LookupTable[row][col] + inc;

        if (val < 0)
            return;

        AllocatePage(val);
        m_LookupTable[row][col] = val;
    }

    public void GetIndexOffset(int i, out int column, out int row, out int dataIndex)
    {
        int channel = (int)Math.Floor((double)i / (double)SONG_DATA_COUNT) % channels;
        row = i / (channels * SONG_DATA_COUNT);
        column = m_LookupTable[currentPattern][channel];
        dataIndex = i % SONG_DATA_COUNT;
    }

    public void AddPatternLine()
    {
        m_LookupTable.Add(new int[channels]);

        for (int j = 0; j < channels; j++)
        {
            int index = GetFirstUnallocatedIndex();
            AllocatePage(index);
            m_LookupTable[m_LookupTable.Count - 1][j] = index;
        }
    }

    public void AllocatePage(int index = -1)
    {
        if (index < 0 || m_SongData.Count >= index)
        {
            m_SongData.Add(new ColumnEntry(m_PatternLength, SONG_DATA_COUNT));
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
