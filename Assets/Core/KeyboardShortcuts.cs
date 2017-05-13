using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KeyboardShortcuts : MonoBehaviour {
    public PatternView patternView;
    public SongData songData;
    public History history;
    public VirtualKeyboard keyboard;
    public SongPlayback playback;
    public TrackerControls controls;
    public float debounceCooldown;
    public float debounceInterval;

    private BoxSelectionRange m_LastCopy;
    private int[,,] m_CopyData;
    private int m_CopyCol;

	// Update is called once per frame
	void Update () {
        if (!playback.isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
                DoShortcut(KeyCode.DownArrow, () => { patternView.MoveVertical(1); });
            if (Input.GetKeyDown(KeyCode.UpArrow))
                DoShortcut(KeyCode.UpArrow, () => { patternView.MoveVertical(-1); });
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                DoShortcut(KeyCode.LeftArrow, () => { patternView.MoveHorizontal(-1); });
            if (Input.GetKeyDown(KeyCode.RightArrow))
                DoShortcut(KeyCode.RightArrow, () => { patternView.MoveHorizontal(1); });
        }

        if (Input.GetKey(KeyCode.LeftControl)) {
            if (Input.GetKeyDown(KeyCode.C))
                CopySelection();
            if (Input.GetKeyDown(KeyCode.V))
                PasteSelection();
            if (Input.GetKeyDown(KeyCode.X)) {
                CopySelection();
                DeleteSelection();
            }

            //if (Input.GetKeyDown(KeyCode.Z))
            //    history.Undo();

            if (Input.GetKeyDown(KeyCode.F1))
                Transpose(-1);
            if (Input.GetKeyDown(KeyCode.F2))
                Transpose(1);
            if (Input.GetKeyDown(KeyCode.F3))
                Transpose(-12);
            if (Input.GetKeyDown(KeyCode.F4))
                Transpose(12);
        }

        if ( patternView.recording ) {
            if ( Input.GetKeyDown ( KeyCode.Delete ) )
                DoShortcut ( KeyCode.Delete, DeleteSelection );

            if ( Input.GetKeyDown ( KeyCode.Backspace ) )
                DoShortcut ( KeyCode.Backspace, Erase );

            if ( Input.GetKeyDown ( KeyCode.Insert ) )
                DoShortcut ( KeyCode.Insert, Insert );
        }

        if (!Input.GetKey(KeyCode.LeftControl))
        {
            for (int i = (int)KeyCode.F1; i <= (int)KeyCode.F8; i++)
            {
                if (Input.GetKeyDown((KeyCode)i))
                {
                    controls.octave.value = i - (int)KeyCode.F1 + 1;
                }
            }
        }
    }

    void DoShortcut(KeyCode key, System.Action action)
    {
        StopAllCoroutines();
        StartCoroutine(DebounceRoutine(key, action));
    }

    IEnumerator DebounceRoutine(KeyCode key, System.Action action)
    {
        if(!Input.GetKeyDown(key))
            yield break;

        action();
        yield return new WaitForSeconds(debounceCooldown);

        while (Input.GetKey(key))
        {
            action();
            yield return new WaitForSeconds(debounceInterval);
        }
    }

    void Transpose(int direction)
    {
        //history.AddHistroyAtSelection ( );

        if (patternView.boxSelection.hasSelection) {
            patternView.boxSelection.DoOperation((int line, int chn, int col) => {
                if (col != 0)
                    return;

                int newNote = TransposeNote(direction, songData.GetData(chn, line, col));
                songData.SetData(chn, line, col, newNote);
            });
        } else if(patternView.selectedChannel == 0) {
            int newNote = TransposeNote(direction, patternView.GetDataAtSelection());
            patternView.SetDataAtSelection(newNote);
        }
    }

    int TransposeNote(int direction, int data)
    {
        if ( data < 0 )
            return data;

        int note = (int)VirtualKeyboard.GetNote(data) - 1;

        if ( note + 1 == ( int ) VirtualKeyboard.Note.NoteOff )
            return data;

        int octave = VirtualKeyboard.GetOctave(data);

        int offset = note + direction;
        if (offset > 11)
        {
            octave++;
        }
        if (offset < 0)
        {
            octave--;
            offset = offset + 12;
        }

        offset = System.Math.Abs(offset) % 12 + 1;

        return VirtualKeyboard.EncodeNoteInfo(offset, octave);
    }

    void CopySelection()
    {
        m_LastCopy = patternView.boxSelection.selection;
        m_CopyCol = m_LastCopy.startCol;
        m_CopyData = new int[m_LastCopy.lineDelta + 1, m_LastCopy.chnDelta + 1, m_LastCopy.colDelta + 1];

        if (patternView.boxSelection.hasSelection) {
            patternView.boxSelection.DoOperation((int line, int chn, int col) => {
                int data = songData.GetData(chn, line, col);

                line -= m_LastCopy.startLine;
                chn -= m_LastCopy.startChn;
                col -= m_LastCopy.startCol;
                try {
                    m_CopyData[line, chn, col] = data;
                } catch (System.IndexOutOfRangeException) {
                    Debug.Log("Index out of range at " + line + ", " + chn + ", " + col);
                }
            }, false);
        } else {
            m_CopyData[0, 0, 0] = patternView.GetDataAtSelection();
        }
    }

    void PasteSelection()
    {
        int lines = m_CopyData.GetLength(0);
        int chns = m_CopyData.GetLength(1);
        int cols = m_CopyData.GetLength(2);

        int currLine = patternView.selectedLine;
        int currChn = patternView.selectedChannel;
        int currCol = patternView.selectedDataColumn;

        //offset currCol to same col as copy
        int colDelta = (currCol % SongData.SONG_DATA_COUNT) - (m_LastCopy.startCol & SongData.SONG_DATA_COUNT);
        currCol -= colDelta;

        for (int line = 0; line < lines; line++) {
            for (int chn = 0; chn < chns; chn++) {
                for (int col = 0; col < cols; col++) {
                    songData.SetData(currChn + chn, currLine + line, currCol + col, m_CopyData[line, chn, col]);
                }

                patternView.UpdateSingleRow(currChn + chn, currLine + line);
            }
        }
    }

    void DeleteSelection()
    {
        if (patternView.boxSelection.hasSelection) {
            patternView.boxSelection.DoOperation((int line, int chn, int col) => {
                songData.SetData(chn, line, col, -1);
            });
        } else {
            patternView.SetDataAtSelection(-1);
            if (patternView.selectedDataColumn == 0)
                patternView.SetDataAtSelection(-1, 1);
            patternView.MoveVertical(1);
        }
    }

    void Erase()
    {
        //history.AddHistroyAtSelection ( );

        if ( patternView.selectedLine == 0 )
            return;

        for (int i = patternView.selectedLine - 1; i < songData.patternLength - 1; i++)
        {
            for (int j = 0; j < SongData.SONG_DATA_COUNT; j++)
            {
                songData.currentColumn.data[i, j] = songData.currentColumn.data[i + 1, j];
            }

            patternView.UpdateSingleRow ( patternView.selectedChannel, i );
        }

        patternView.MoveVertical ( -1 );
    }

    void Insert()
    {
        //history.AddHistroyAtSelection ( );

        for (int i = songData.patternLength - 1; i >= patternView.selectedLine ; i--)
        {
            for (int j = 0; j < SongData.SONG_DATA_COUNT; j++)
            {
                if (i == patternView.selectedLine )
                    songData.currentColumn.data[i, j] = -1;
                else
                    songData.currentColumn.data[i, j] = songData.currentColumn.data[i - 1, j];
            }

            patternView.UpdateSingleRow ( patternView.selectedChannel, i );
        }

        patternView.MoveVertical ( 1 );
    }
}
