using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KeyboardShortcuts : MonoBehaviour {
    public PatternView patternView;
    public SongData songData;
    public History history;
    public VirtualKeyboard keyboard;
    public SongPlayback playback;
    public float debounceCooldown;
    public float debounceInterval;

    private List<int> m_CopyData = new List<int>();
    private int m_CopyOffset;

	// Update is called once per frame
	void Update () {
        if (!playback.isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
                DoShortcut(KeyCode.DownArrow, () => { patternView.MoveLine(1); });
            if (Input.GetKeyDown(KeyCode.UpArrow))
                DoShortcut(KeyCode.UpArrow, () => { patternView.MoveLine(-1); });
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                DoShortcut(KeyCode.LeftArrow, () => { patternView.MoveColumn(-1); });
            if (Input.GetKeyDown(KeyCode.RightArrow))
                DoShortcut(KeyCode.RightArrow, () => { patternView.MoveColumn(1); });
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.C))
                CopySelection();
            if (Input.GetKeyDown(KeyCode.V))
                PasteSelection();
            if(Input.GetKeyDown(KeyCode.X))
            {
                CopySelection();
                DeleteSelection();
            }

            if (Input.GetKeyDown(KeyCode.Z))
                history.Undo();

            if (Input.GetKeyDown(KeyCode.F1))
                Transpose(-1);
            if (Input.GetKeyDown(KeyCode.F2))
                Transpose(1);
            if (Input.GetKeyDown(KeyCode.F3))
                Transpose(-12);
            if (Input.GetKeyDown(KeyCode.F4))
                Transpose(12);
        }

        if (patternView.keyboard.recording)
        {
            if (Input.GetKeyDown(KeyCode.Delete))
                DoShortcut(KeyCode.Delete, DeleteSelection);

            if (Input.GetKeyDown(KeyCode.Backspace))
                DoShortcut(KeyCode.Backspace, Erase);

            if (Input.GetKeyDown(KeyCode.Insert))
                DoShortcut(KeyCode.Insert, Insert);
        }

        if (!Input.GetKey(KeyCode.LeftControl))
        {
            for (int i = (int)KeyCode.F1; i <= (int)KeyCode.F8; i++)
            {
                if (Input.GetKeyDown((KeyCode)i))
                {
                    keyboard.currentOctave = i - (int)KeyCode.F1 + 1;
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
        history.AddHistroyAtSelection ( );

        if (patternView.multipleSelection)
        {
            for (int i = 0; i < patternView.length; i++)
            {
                if (i % SongData.SONG_DATA_COUNT == 0 && patternView.IsInSelection(i))
                {
                    songData[i] = TransposeNote(direction, songData[i]);
                }
            }
        }
        else if(patternView.selection % SongData.SONG_DATA_COUNT == 0)
        {
            songData [patternView.selection] = TransposeNote(direction, songData[patternView.selection]);
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
        m_CopyData.Clear();

        if (patternView.multipleSelection)
        {
            for (int i = 0; i < patternView.length; i++)
            {
                if (patternView.IsInSelection(i))
                {
                    m_CopyData.Add(songData[i]);
                }
            }

            m_CopyOffset = patternView.dragSelectOffset;
        }
        else
        {
            m_CopyData.Add(songData.currentColumn.data[patternView.currentLine, patternView.selectedAttribute]);
        }

        Debug.Log("Copied " + m_CopyData.Count + " entries");
    }

    void PasteSelection()
    {
        if(m_CopyData.Count > 1)
        {
            history.AddHistoryEntry ( patternView.GetChannelSelection ( patternView.selection ), patternView.GetChannelSelection ( patternView.selection + m_CopyOffset) );


            int cpy = 0;
            int startLine = -1;
            int line = -1;
            patternView.SetDragSelection(patternView.selection, m_CopyOffset);
            for (int i = 0; i < patternView.length; i++)
            {
                if (patternView.IsInSelection(i))
                {
                    line = i / patternView.lineOffset;
                    if (startLine < 0)
                        startLine = line;

                    songData[i] = m_CopyData[cpy];
                    cpy++;
                }
            }

            patternView.MoveLine(line - startLine + 1);
            patternView.SetDragSelection(patternView.selection, 0);
        }
        else if(m_CopyData.Count > 0)
        {
            history.AddHistroyAtSelection ( );
            songData.currentColumn.data[patternView.currentLine, patternView.selectedAttribute] = m_CopyData[0];
            patternView.MoveLine(1);
        }
    }

    void DeleteSelection()
    {
        history.AddHistroyAtSelection ( );
        if (patternView.multipleSelection)
        {
            for (int i = 0; i < patternView.length; i++)
            {
                if (patternView.IsInSelection(i))
                {
                    songData[i] = -1;
                }
            }
        }
        else
        {
            songData.currentColumn.data[patternView.currentLine, patternView.selectedAttribute] = -1;
            if (patternView.selectedAttribute == 0)
                songData.currentColumn.data[patternView.currentLine, 1] = -1;
            patternView.MoveLine(1);
        }
    }

    void Erase()
    {
        history.AddHistroyAtSelection ( );

        for (int i = patternView.currentLine; i < songData.patternLength - 1; i++)
        {
            for (int j = 0; j < SongData.SONG_DATA_COUNT; j++)
            {
                songData.currentColumn.data[i, j] = songData.currentColumn.data[i + 1, j];
            }
        }
    }

    void Insert()
    {
        history.AddHistroyAtSelection ( );

        for (int i = songData.patternLength - 1; i >= patternView.currentLine; i--)
        {
            for (int j = 0; j < SongData.SONG_DATA_COUNT; j++)
            {
                if (i == patternView.currentLine)
                    songData.currentColumn.data[i, j] = -1;
                else
                    songData.currentColumn.data[i, j] = songData.currentColumn.data[i - 1, j];
            }
        }
    }
}
