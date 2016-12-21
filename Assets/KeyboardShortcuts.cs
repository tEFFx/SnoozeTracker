using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KeyboardShortcuts : MonoBehaviour {
    public PatternView patternView;
    public SongData songData;
    public History history;

    private List<int> m_CopyData = new List<int>();
    private int m_CopyOffset;

	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.DownArrow))
            patternView.MoveLine(1);
        if (Input.GetKeyDown(KeyCode.UpArrow))
            patternView.MoveLine(-1);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            patternView.MoveColumn(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            patternView.MoveColumn(1);

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
                DeleteSelection();

            if (Input.GetKeyDown(KeyCode.Backspace))
                Erase();

            if (Input.GetKeyDown(KeyCode.Insert))
                Insert();
        }
    }

    void Transpose(int direction)
    {
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
            songData[patternView.selection] = TransposeNote(direction, songData[patternView.selection]);
        }
    }

    int TransposeNote(int direction, int data)
    {
        int note = (int)VirtualKeyboard.GetNote(data) - 1;
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
            songData.currentColumn.data[patternView.currentLine, patternView.selectedAttribute] = m_CopyData[0];
            patternView.MoveLine(1);
        }
    }

    void DeleteSelection()
    {
        if (patternView.multipleSelection)
        {
            history.AddHistoryEntry(patternView.GetChannelSelection(patternView.dragSelectStart), patternView.GetChannelSelection(patternView.dragSelectStart + patternView.dragSelectOffset));

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
            history.AddHistoryEntry(patternView.selectedChannel);

            songData.currentColumn.data[patternView.currentLine, patternView.selectedAttribute] = -1;
            if (patternView.selectedAttribute == 0)
                songData.currentColumn.data[patternView.currentLine, 1] = -1;
            patternView.MoveLine(1);
        }
    }

    void Erase()
    {
        history.AddHistoryEntry(patternView.selectedChannel);

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
        history.AddHistoryEntry(patternView.selectedChannel);

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
