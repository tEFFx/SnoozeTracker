using UnityEngine;
using System.Collections;
using System;

public class PatternView : MonoBehaviour {
    public int lineOffset { get { return data.channels * SongData.SONG_DATA_COUNT; } }
    public int length { get { return data.channels * data.lines * SongData.SONG_DATA_COUNT; } }
    public int currentLine { get { return (m_Selection / lineOffset); } }
    public int selection { get { return m_Selection; } }
    public int selectedChannel { get { return (int)Math.Floor((double)m_Selection / (double)SongData.SONG_DATA_COUNT) % data.channels; } }

    public SongData data;
    public float[] lineWidths;
    public float lineHeight;
    public float channelSpacing;
    public Vector2 padding;
    public Color selectionColor;
    public Color lineColor;
    public Color neutralColor;

    private int m_Selection;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
            MoveLine(1);
        if (Input.GetKeyDown(KeyCode.UpArrow))
            MoveLine(-1);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            m_Selection += (m_Selection % lineOffset == 0) ? lineOffset - 1 : -1;
        if (Input.GetKeyDown(KeyCode.RightArrow))
            m_Selection += (m_Selection % lineOffset == lineOffset - 1) ? -lineOffset + 1 : 1;
    }

	void OnGUI()
    {
        Rect area = new Rect(padding, new Vector2(Screen.width, Screen.height) - padding);
        GUILayout.BeginArea(area);

        for(int i = 0; i < length; i++)
        {
            int lineNr = i / lineOffset;

            if (i % lineOffset == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUI.color = Color.white;
                GUILayout.Box(lineNr.ToString("X2"), GUILayout.Width(36), GUILayout.Height(lineHeight));
            }

            GUI.color = lineNr == currentLine ? lineColor : neutralColor;
            GUI.color = i == m_Selection ? selectionColor : GUI.color;

            int wId = i % SongData.SONG_DATA_COUNT;
            string text;

            switch (wId)
            {
                case 0:
                    text = VirtualKeyboard.FormatNote(data[i]);
                    break;
                case 2:
                    text = data[i].ToString("X");
                    break;
                default:
                    text = data[i].ToString("X2");
                    break;
            }

            if (GUILayout.Button(text, GUILayout.Width(lineWidths[wId]), GUILayout.Height(lineHeight)))
            {
                m_Selection = i;
            }

            if (wId == lineWidths.Length - 1)
                GUILayout.Space(channelSpacing);
        }

        GUILayout.EndArea();
    }

    public void MoveLine(int lines = 1)
    {
        m_Selection += lines * lineOffset;

        if (m_Selection >= length)
            m_Selection = m_Selection - length;
        if (m_Selection < 0)
            m_Selection = m_Selection + length;

        //Debug.Log(m_Selection);
    }
}
