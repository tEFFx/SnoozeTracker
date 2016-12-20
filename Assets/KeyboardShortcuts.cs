using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KeyboardShortcuts : MonoBehaviour {
    public PatternView patternView;
    public SongData songData;

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
