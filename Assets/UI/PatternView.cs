using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PatternView : MonoBehaviour {
    public int selectedLine {
        get { return m_CurrentLine; }
        set {
            SetSelection ( value );
        }
    }
    public int selectedChannel { get { return m_CurrentChannel; } }
    public int selectedDataColumn { get { return m_CurrentColumn; } }

    public Transform[] channels;
    public Transform lineNumbers;
    public GameObject lineNumberPrefab;
    public GameObject patternRowPrefab;
    public SongData data;
    public SongPlayback playback;
    public Instruments instruments;
    public ScrollRect scroll;
    public Image selection;
    public float lineHeight;
    public Color selectionRecording;
    public Color selectionNormal;
    public BoxSelection boxSelection;


    [HideInInspector]
    public bool recording;

    private int m_CurrentLength;
    private int m_CurrentLine;
    private int m_CurrentChannel;
    private int m_CurrentColumn;
    private string m_Input;
    private int m_InputPos;

    private List<Text> m_LineNumbers = new List<Text>();
    private List<PatternRow>[] m_PatternRows;

    void Awake() {
        m_PatternRows = new List<PatternRow> [ channels.Length ];
        for ( int i = 0 ; i < channels.Length ; i++ ) {
            m_PatternRows [ i ] = new List<PatternRow> ( );
        }
    }

	// Use this for initialization
	void Start () {
        UpdatePatternView ( );
        SetSelection ( 0, 0 );
	}
	
	// Update is called once per frame
	void Update () {
        UpdatePatternView ( );

        if ( Input.GetKeyDown ( KeyCode.Space ) ) {
            selection.color = recording ? selectionRecording : selectionNormal;
            recording = !recording;
        }

        if (recording) {
            int maxLen = selectedDataColumn == 2 ? 1 : 2;

            if (selectedDataColumn != 0 && Input.inputString.Length > 0) {
                if(selectedLine + selectedDataColumn != m_InputPos) {
                    m_InputPos = selectedLine + selectedDataColumn;
                    m_Input = System.String.Empty;
                }
                m_Input += Input.inputString[0];

                int res;
                if(int.TryParse(m_Input, System.Globalization.NumberStyles.HexNumber, null, out res)) {
                    SetDataAtSelection(res);
                } else {
                    m_Input = m_Input.Substring(0, m_Input.Length - 1);
                }

                if (m_Input.Length >= maxLen) {
                    MoveVertical(1);
                    m_Input = System.String.Empty;
                }

            }
        }
    }

    private void UpdatePatternView() {
        if ( m_CurrentLength == data.patternLength )
            return;

        if ( m_CurrentLength < data.patternLength ) {
            for ( int i = 0 ; i < data.patternLength - m_CurrentLength; i++ ) {
                GameObject lineNum = Instantiate ( lineNumberPrefab, lineNumbers );
                m_LineNumbers.Add ( lineNum.GetComponentInChildren<Text> ( ) );

                for ( int p = 0 ; p < channels.Length ; p++ ) {
                    GameObject rowObj = Instantiate ( patternRowPrefab, channels [ p ] );
                    PatternRow row = rowObj.GetComponent<PatternRow> ( );
                    row.view = this;
                    row.channel = p;
                    row.UpdateData ( );
                    m_PatternRows[p].Add ( row );
                }
            }
        } else {
            int removeCount = m_CurrentLength - data.patternLength;
            for ( int i = 0 ; i < removeCount ; i++ ) {
                Destroy ( m_LineNumbers [ i ] );
            }

            m_LineNumbers.RemoveRange ( 0, removeCount );

            removeCount *= channels.Length;
            for ( int i = 0 ; i < channels.Length ; i++ ) {
                for ( int p = 0 ; p < removeCount ; p++ ) {
                    Destroy ( m_PatternRows [ i ] [ p ] );
                }

                m_PatternRows[i].RemoveRange ( 0, removeCount );
            }

            UpdatePatternData ( );
        }

        UpdateLineNumbers ( );
        m_CurrentLength = data.patternLength;
    }

    public void UpdatePatternData() {
        Debug.Log ( "Updating data" );
        for ( int i = 0 ; i < data.patternLength ; i++ ) {
            m_PatternRows [ 0 ] [ i ].UpdateData ( );
            m_PatternRows [ 1 ] [ i ].UpdateData ( );
            m_PatternRows [ 2 ] [ i ].UpdateData ( );
            m_PatternRows [ 3 ] [ i ].UpdateData ( );
        }
    }

    public void UpdatePatternChannel(int channel) {
        for (int i = 0; i < data.patternLength; i++) {
            m_PatternRows[channel][i].UpdateData();
        }
    }
    
    public void UpdateSelection() {
        UpdateSingleRow ( m_CurrentChannel, m_CurrentLine );
    }

    public void UpdateSingleRow(int channel, int line) {
        m_PatternRows [ channel ] [ line ].UpdateData ( );
    }

    public void SetDataAtSelection(int data, int colOffset = 0) {
        this.data.SetData ( m_CurrentChannel, m_CurrentLine, m_CurrentColumn + colOffset, data );
        UpdateSelection ( );
    }

    public int GetDataAtSelection(int colOffset = 0) {
        return data.GetData(m_CurrentChannel, m_CurrentLine, m_CurrentColumn + colOffset);
    }

    private void UpdateLineNumbers() {
        for ( int i = 0 ; i < m_LineNumbers.Count ; i++ ) {
            m_LineNumbers [ i ].text = i.ToString ( "X2" );
        }
    }

    public void MoveVertical(int increment) {
        int line = m_CurrentLine + increment;

        if ( line > data.patternLength )
            line = 0;
        else if(line < 0)
            line = data.patternLength - 1;

        SetSelection ( line );
    }

    public void MoveHorizontal(int increment) {
        int column = m_CurrentColumn + increment;
        int channel = m_CurrentChannel;

        if ( column >= PatternRow.numDataEntries ) {
            column = 0;
            channel++;
        } else if ( column < 0 ) {
            column = PatternRow.numDataEntries - 1;
            channel--;
        }

        if ( channel >= channels.Length ) {
            channel = 0;
        } else if ( channel < 0 ) {
            channel = channels.Length - 1;
        }

        SetSelection ( m_CurrentLine, channel, column );
    }

    public void SetSelection(int line, int channel = -1, int column = -1) {
        if ( line >= data.patternLength )
            line = 0;

        m_PatternRows [ m_CurrentChannel ] [ m_CurrentLine ].Deselect ( );
        m_CurrentLine = line;

        if (channel >= 0) {
            m_CurrentChannel = channel;
            if (column >= 0)
                m_CurrentColumn = column;
        }

        Vector2 selPos = selection.rectTransform.anchoredPosition;
        Vector3 scrollPos = scroll.content.localPosition;

        RectTransform parentRect = transform.parent.GetComponent<RectTransform>();

        if (playback.isPlaying) {
            if (scroll.enabled) {
                scroll.enabled = false;
                selection.transform.SetParent(transform.parent);
            }

            float offset = -parentRect.rect.height * 0.5f;
            scrollPos.y = -m_PatternRows[m_CurrentChannel][m_CurrentLine].transform.localPosition.y + offset;
            scroll.verticalScrollbar.value = 1 - ((float)m_CurrentLine / data.patternLength);
            selPos.y = offset + 10;
        } else {
            selPos.y = -lineHeight * line;
            if (!scroll.enabled) {
                scroll.enabled = true;
                selection.transform.SetParent(transform);
            }
        }

        scroll.content.localPosition = scrollPos;
        selection.rectTransform.anchoredPosition = selPos;
        m_PatternRows[ m_CurrentChannel ] [ m_CurrentLine ].Select ( m_CurrentColumn );
    }
}
