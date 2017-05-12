using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PatternViewLegacy : MonoBehaviour {
    public int lineOffset { get { return data.channels * SongData.SONG_DATA_COUNT; } }
    public int length { get { return data.channels * data.patternLength * SongData.SONG_DATA_COUNT; } }
    public int currentLine { get { return (m_Selection / lineOffset); } }
    public int selection { get { return m_Selection; } }
    public int selectedChannel { get { return GetChannelSelection(m_Selection); } }
    public int selectedAttribute { get { return m_Selection % SongData.SONG_DATA_COUNT; } }
    public bool multipleSelection { get { return m_DragSelectStart != m_DragSelectEnd; } }
    public int dragSelectStart { get { return m_DragSelectStart; } }
    public int dragSelectOffset { get { return m_DragSelectEnd - m_DragSelectStart; } }

    public SongData data;
    public SongPlayback playback;
    public VirtualKeyboard keyboard;
    public Instruments instruments;
    public float[] lineWidths;
    public float lineHeight;
    public float channelSpacing;
    public Vector2 padding;
    public Color recordColor;
    public Color lineColor;
    public Color multipleSelectColor;
    public Color neutralColor;
    public Color lineHighlightColor;
    public Color effectColor;
    public Gradient transposeGradient;
    public Gradient volumeGradient;
    public Gradient instrumentGradient;
    public GUISkin skin;

    private int m_Selection;
    private int m_LastSelection;
    private int m_InputSelection;
    private string m_Input = "";
    private char m_LastChar;

    private bool m_Dragging;
    private int m_DragSelectStart;
    private int m_DragSelectEnd;
    private Vector2 m_Scroll = new Vector2();

    private Texture2D m_VolBox;

    void Start()
    {
        m_VolBox = new Texture2D(1, 1);
        m_VolBox.SetPixel(0, 0, Color.green);
        m_VolBox.Apply();
    }

    void Update()
    {
        //if (keyboard.recording) {
        //    int maxLen = lineWidths[selectedAttribute % lineOffset] < 1 ? 1 : 2;
        //    if (m_Input.Length >= maxLen || m_Selection != m_InputSelection)
        //        m_Input = "";

        //    if (selectedAttribute != 0 && Input.inputString.Length > 0 && m_LastChar != Input.inputString[0])
        //    {
        //        m_InputSelection = m_Selection;
        //        m_LastChar = Input.inputString[0];
        //        m_Input += m_LastChar;

        //        int res;
        //        if (int.TryParse(m_Input, System.Globalization.NumberStyles.HexNumber, null, out res))
        //        {
        //            //data[selection] = (byte)res;
        //        }

        //        if (m_Input.Length >= maxLen)
        //            MoveLine(1);

        //    }
        //    else if (Input.inputString.Length == 0 && m_LastChar != 0)
        //    {
        //        m_LastChar = (char)0;
        //    }

        //    if (Input.GetKeyDown(KeyCode.Return))
        //        m_Input = "";
        //}

        if ( playback.isPlaying ) {
            m_Scroll.y = currentLine * lineHeight - (Screen.height - padding.y) * 0.5f;
        }
    }

	void OnGUI()
    {
        GUI.skin = skin;

        Vector2 pos = padding;
        Vector2 size = new Vector2 ( 32, lineHeight );
        float chnlWidth = 0;
        for ( int i = 0 ; i < lineWidths.Length ; i++ ) {
            chnlWidth += lineWidths [ i ] * size.x;
        }

        pos.x += size.x;
        for ( int i = 0 ; i < data.channels; i++ ) {
            string buttonText = "PSG" + i;
            if ( playback.mute [ i ] )
                buttonText += "(muted)";
            Rect buttonRect = new Rect(pos, new Vector2(chnlWidth, size.y));
            Rect attnRect = buttonRect;

            attnRect.width = attnRect.width * ((playback.chnAttn[i] / 16f));
            attnRect.x += (buttonRect.width - attnRect.width) * 0.5f * 0.95f;

            GUI.DrawTexture(attnRect, m_VolBox);

            if (GUI.Button(buttonRect, buttonText))
                playback.mute[i] = !playback.mute[i];
            pos.x += chnlWidth + channelSpacing;
        }

        Rect scrollRect = new Rect ( padding.x, pos.y + size.y, Screen.width, Screen.height - padding.y );
        Rect viewRect = new Rect ( 0, 0, data.channels * chnlWidth + size.x,  (playback.isPlaying ? scrollRect.height : ( data.patternLength * size.y + size.y ) ) );
        Vector2 scroll = GUI.BeginScrollView (scrollRect, m_Scroll, viewRect );
        if ( !playback.isPlaying )
            m_Scroll = scroll;

        Rect clipRect = new Rect ( m_Scroll.x, ( !playback.isPlaying ? m_Scroll.y : 0 ), Screen.width, (!playback.isPlaying ? scrollRect.height : viewRect.height));
        pos.y = (playback.isPlaying ? -size.y * 2 - (currentLine * size.y - scrollRect.height / 2) : -size.y );
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


            //Color sel = keyboard.recording ? recordColor : lineColor;
            //Color line = sel;
            //line.a = 0.5f;

            //Color bg = lineNr % 8 == 0 ? lineHighlightColor : neutralColor;
            //bg = IsInSelection ( i ) && m_DragSelectStart != m_DragSelectEnd ? multipleSelectColor : bg;
            //bg = lineNr == currentLine ? line : bg;
            //bg = i == m_Selection ? sel : bg;
            //GUI.backgroundColor = bg;

            int wId = i % SongData.SONG_DATA_COUNT;
            string text = "-";

            //if ( data [ i ] >= 0 ) {
            //    switch ( wId ) {
            //        case 0:
            //            text = VirtualKeyboard.FormatNote ( data [ i ] );

            //            int off = data.GetTransposeOffset ( i );
            //            off = Mathf.Clamp ( ( off + 12 ) / 2, 0, 12 );

            //            GUI.contentColor = transposeGradient.Evaluate ( off / 12f );
            //            break;
            //        case 1:
            //            if ( data [ i ] != -1 )
            //                GUI.contentColor = instrumentGradient.Evaluate ( data [ i ] < instruments.presets.Count ? 1 : 0 );
            //            text = data [ i ].ToString ( "X2" );
            //            break;
            //        case 2:
            //            if ( data [ i ] != -1 )
            //                GUI.contentColor = volumeGradient.Evaluate ( data [ i ] / 15.0f );
            //            text = data [ i ].ToString ( "X" );
            //            break;
            //        case 3:
            //            if ( data [ i ] != -1 )
            //                GUI.contentColor = effectColor;
            //            text = data [ i ].ToString ( "X2" );
            //            break;
            //        case 4:
            //            if ( data [ i ] != -1 )
            //                GUI.contentColor = effectColor;
            //            text = data [ i ].ToString ( "X2" );
            //            break;
            //    }
            //}else if(data[i] == -2 ) {
            //    text = "-";
            //    GUI.enabled = false;
            //}

            Rect buttonRect = new Rect ( pos, new Vector2 ( size.x * lineWidths [ wId ], size.y ) );
            if ( !buttonRect.Overlaps ( clipRect ) ) {
                continue;
            }

            GUI.Button(buttonRect, text);
            GUI.contentColor = Color.white;

            GUI.enabled = true;

            pos.x += size.x * lineWidths [ wId ];
            if ( wId == 4 )
                pos.x += channelSpacing;

            if ( !playback.isPlaying ) {
                if ( Input.GetMouseButtonDown ( 0 ) ) {
                    Vector2 mPos = Event.current.mousePosition;
                    if ( buttonRect.Contains ( mPos )) {
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
        }

        GUI.EndScrollView ( );
    }

    public int GetChannelSelection(int i)
    {
        return (int)Math.Floor((double)i / (double)SongData.SONG_DATA_COUNT) % data.channels;
    }

    public bool IsInSelection(int i) {
        if ( m_DragSelectStart == m_DragSelectEnd )
            return false;

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

    public void SetDragSelection(int _pos, int _offset)
    {
        m_DragSelectStart = _pos;
        m_DragSelectEnd = _pos + _offset;
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

    public void MoveColumn(int cols = 1)
    {
        if(cols < 0)
            m_Selection += (m_Selection % lineOffset == 0) ? lineOffset - 1 : -1;
        else
            m_Selection += (m_Selection % lineOffset == lineOffset - 1) ? -lineOffset + 1 : 1;
    }
}
