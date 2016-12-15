using UnityEngine;
using System.Collections;

public class KeyboardShortcuts : MonoBehaviour {
    public PatternView patternView;
    public SongData songData;

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

        if (patternView.keyboard.recording)
        {
            if (Input.GetKeyDown(KeyCode.Delete))
                DeleteSelection();
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
}
