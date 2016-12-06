using UnityEngine;
using System.Collections;
using System;

public class PatternView : MonoBehaviour {
    public int lineOffset { get { return data.channels * SongData.SONG_DATA_COUNT; } }
    public int length { get { return data.channels * data.patternLength * SongData.SONG_DATA_COUNT; } }
    public int currentLine { get { return (m_Selection / lineOffset); } }
    public int selection { get { return m_Selection; } }
    public int selectedChannel { get { return (int)Math.Floor((double)m_Selection / (double)SongData.SONG_DATA_COUNT) % data.channels; } }
    public int selectedAttribute { get { return m_Selection % SongData.SONG_DATA_COUNT; } }

    public SongData data;
    public SongPlayback playback;
    public float[] lineWidths;
    public float lineHeight;
    public float channelSpacing;
    public Vector2 padding;
    public Color selectionColor;
    public Color multipleSelectColor;
    public Color lineColor;
    public Color neutralColor;

    private int m_Selection;
    private int m_LastSelection;
    private int m_InputSelection;
    private string m_Input = "";
    private char m_LastChar;

    private bool m_Dragging;
    private int m_DragSelectStart;
    private int m_DragSelectEnd;
    private Vector2 m_Scroll = new Vector2();

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

        if ( selectedAttribute != 0 && Input.inputString.Length > 0 && m_LastChar != Input.inputString[0]) {
            int maxLen = lineWidths[selectedAttribute % lineOffset] < 1 ? 1 : 2;

            if ( m_Input.Length >= maxLen || m_Selection != m_InputSelection )
                m_Input = "";

            m_InputSelection = m_Selection;
            m_LastChar = Input.inputString [ 0 ];
            m_Input += m_LastChar;

            int res;
            if(int.TryParse(m_Input, System.Globalization.NumberStyles.HexNumber, null, out res ) ) {
                data [ selection ] = (byte)res;
            }

            if ( m_Input.Length >= maxLen )
                MoveLine ( 1 );

        } else if(Input.inputString.Length == 0 && m_LastChar != 0 ) {
            m_LastChar = (char)0;
        }

        if ( Input.GetKeyDown ( KeyCode.Return ) )
            m_Input = "";
    }

	void OnGUI()
    {
        Vector2 pos = padding;
        Vector2 size = new Vector2 ( 32, 24 );
        float chnlWidth = 0;
        for ( int i = 0 ; i < lineWidths.Length ; i++ ) {
            chnlWidth += lineWidths [ i ] * size.x;
        }

        pos.x += size.x;
        for ( int i = 0 ; i < data.channels; i++ ) {
            string buttonText = "PSG" + i;
            if ( playback.mute [ i ] )
                buttonText += "(muted)";
            if ( GUI.Button ( new Rect ( pos, new Vector2 ( chnlWidth, size.y ) ), buttonText) )
                playback.mute [ i ] = !playback.mute [ i ];
            pos.x += chnlWidth + channelSpacing;
        }

        m_Scroll = GUI.BeginScrollView ( new Rect ( padding.x, pos.y + size.y, Screen.width, Screen.height - padding.y ), m_Scroll, new Rect(0, 0, data.channels * chnlWidth + size.x, data.patternLength * size.y + size.y) );
        pos.y = -size.y;
        for ( int i = 0; i < length; i++)
        {
            int lineNr = i / lineOffset;

            if (i % lineOffset == 0)
            {
                pos.y += size.y;
                pos.x = padding.x;
                GUI.backgroundColor = Color.white;
                GUI.Box(new Rect(pos, size), lineNr.ToString("X2"));
                pos.x += size.x;
            }


            GUI.backgroundColor = lineNr == currentLine ? lineColor : neutralColor;
            GUI.backgroundColor = IsInSelection ( i ) && m_DragSelectStart != m_DragSelectEnd ? multipleSelectColor : GUI.backgroundColor;
            GUI.backgroundColor = i == m_Selection ? selectionColor : GUI.backgroundColor;

            int wId = i % SongData.SONG_DATA_COUNT;
            string text;

            if ( data [ i ] < 0 ) {
                text = "-";
            } else {
                switch ( wId ) {
                    case 0:
                        text = VirtualKeyboard.FormatNote ( data [ i ] );
                        break;
                    case 2:
                        text = data [ i ].ToString ( "X" );
                        break;
                    default:
                        text = data [ i ].ToString ( "X2" );
                        break;
                }
            }

            Rect buttonRect = new Rect ( pos, new Vector2 ( size.x * lineWidths [ wId ], size.y ) );
            GUI.Button(buttonRect, text);
            pos.x += size.x * lineWidths [ wId ];
            if ( wId == 4 )
                pos.x += channelSpacing;

            if(Input.GetMouseButtonDown(0)) {
                Vector2 mPos = Event.current.mousePosition;
                if ( buttonRect.Contains ( mPos ) ) {
                    m_LastSelection = m_Selection;
                    m_Selection = i;
                    m_DragSelectStart = m_DragSelectEnd = i;
                    m_Dragging = true;
                }
            }

            if ( m_Dragging ) {
                Vector2 mPos = Event.current.mousePosition;
                if ( buttonRect.Contains ( mPos ) ) {
                    m_DragSelectEnd = i;
                }
            }

            if ( Input.GetMouseButtonUp ( 0 ) ) {
                m_Dragging = false;
            }

        }

        GUI.EndScrollView ( );
    }

    bool IsInSelection(int i) {
        bool res = false;

        int startCol = m_DragSelectStart % lineOffset;
        int endCol = m_DragSelectEnd % lineOffset;
        int iCol = i % lineOffset;

        if ( startCol < endCol ) {
            res = iCol >= startCol && iCol <= endCol;
        } else {
            res = iCol >= endCol && iCol <= startCol;
        }

        int startRow = m_DragSelectStart / lineOffset;
        int endRow = m_DragSelectEnd / lineOffset;
        int iRow = i / lineOffset;

        if ( startRow < endRow ) {
            res &= iRow >= startRow && iRow <= endRow;
        } else {
            res &= iRow >= endRow && iRow <= startRow;
        }

        return res;
            // && i % lineOffset >= m_DragSelectEnd % lineOffset && i % lineOffset <= m_DragSelectStart % lineOffset );
        //&& i % lineOffset >= m_DragSelectStart % lineOffset && i % lineOffset <= m_DragSelectEnd % lineOffset )
    }

    public void MoveLine(int lines = 1)
    {
        m_LastSelection = m_Selection;
        m_Selection += lines * lineOffset;

        if (m_Selection >= length)
            m_Selection = m_Selection - length;
        if (m_Selection < 0)
            m_Selection = m_Selection + length;

        //Debug.Log(m_Selection);
    }
}
