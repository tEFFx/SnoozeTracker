using UnityEngine;
using System.Collections;

public class SongPlayback : MonoBehaviour {
    public PSGWrapper psg;
    public SongData data;
    public PatternView view;
    public Instruments instruments;
    public int playbackSpeed;

    private int m_CurrentPattern;
    private int m_CurrentLine;
    private int m_Counter;
    private long m_StartSample;
    private bool m_IsPlaying;
    private float m_LastLineTick;
    private int m_MoveLine;
    private VirtualKeyboard.Note[] m_CurrentNotes;
    private int[] m_CurrentOctaves;
    private Instruments.InstrumentInstance[] m_Instruments;

    void Start()
    {
        psg.AddIrqCallback(50, OnIrqCallback);
        m_Instruments = new Instruments.InstrumentInstance [ data.channels ];
        m_CurrentOctaves = new int [ data.channels ];
        m_CurrentNotes = new VirtualKeyboard.Note [ data.channels ];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (m_IsPlaying)
                Stop();
            else
                Play();
        }

        if(m_IsPlaying && Time.time - m_LastLineTick > 1f / 50f) {
            while ( m_MoveLine > 0 ) {
                m_MoveLine--;
                view.MoveLine ( 1 );
                if ( view.currentLine == 0 ) {
                    data.currentPattern++;
                    if ( data.currentPattern >= data.numPatterns )
                        data.currentPattern = 0;
                }
                m_LastLineTick = Time.time;
            }
        }
    }

    public void OnIrqCallback()
    {
        if (!m_IsPlaying)
            return;

        m_Counter++;
        if(m_Counter >= playbackSpeed)
        {
            m_Counter = 0;
            for (int i = 0; i < data.channels; i++)
            {
                SongData.ColumnEntry col = data.GetCurrentLine(m_CurrentPattern, i);
                VirtualKeyboard.Note note = VirtualKeyboard.GetNote(col.data[m_CurrentLine, 0]);
                if(note == VirtualKeyboard.Note.NoteOff)
                {
                    m_CurrentNotes[i] = VirtualKeyboard.Note.NoteOff;
                    psg.SetAttenuation(i, 0);
                }else if(note != VirtualKeyboard.Note.None)
                {
                    m_CurrentNotes [ i ] = note;
                    m_CurrentOctaves [ i ] = VirtualKeyboard.GetOctave ( col.data [ m_CurrentLine, 0 ] );
                    m_Instruments [ i ] = instruments.presets [ 0 ];

                    psg.SetFrequency ( i, ( int ) m_CurrentNotes [ i ], m_CurrentOctaves [ i ] );
                }
            }


            m_CurrentLine++;
            m_MoveLine++;
            if(m_CurrentLine >= data.lines)
            {
                m_CurrentLine = 0;
                m_CurrentPattern++;
                if (m_CurrentPattern >= data.numPatterns)
                    m_CurrentPattern = 0;
            }
        }

        for ( int i = 0 ; i < data.channels ; i++ ) {
            if ( m_CurrentNotes [ i ] != VirtualKeyboard.Note.None ) {
                psg.SetAttenuation ( i, m_Instruments [ i ].GetCurrentVol ( ) );

                if(m_Instruments[i].updatesFrequency)
                    psg.SetFrequency ( i, (int)m_CurrentNotes [ i ] + m_Instruments[i].GetNoteOffset(), m_CurrentOctaves [ i ] );
                m_Instruments [ i ].Clock ( );
            }
        }
    }

    public void Play()
    {
        m_StartSample = psg.currentSample;
        m_CurrentPattern = data.currentPattern;
        m_CurrentLine = 0;
        view.MoveLine ( -view.currentLine );
        m_LastLineTick = Time.time;
        m_IsPlaying = true;
    }

    public void Stop()
    {
        m_IsPlaying = false;
        psg.Mute();
    }
}
