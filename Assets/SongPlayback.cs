using UnityEngine;
using System.Collections;

public class SongPlayback : MonoBehaviour {
    public bool isPlaying { get { return m_IsPlaying; } }
    public int playbackRate { get { return m_PlaybackRate; } }
    public int patternLoop { get { return m_PatternLoop; } }
    public int currentPattern { get { return m_PlayingPattern; } }

    public PSGWrapper psg;
    public SongData data;
    public PatternView view;
    public Instruments instruments;
    public int playbackSpeed;
    public bool[] mute;

    private int m_CurrentPattern;
    private int m_PlayingPattern;
    private int m_CurrentLine;
    private int m_Counter;
    private long m_StartSample;
    private bool m_IsPlaying;
    private bool m_IsStopping;
    private float m_LastLineTick;
    private int m_MoveLine;
    private Instruments.InstrumentInstance[] m_Instruments;
    private Instruments.InstrumentInstance[] m_PrevInstruments;
    private bool m_NoiseFB;
    private bool m_NoiseChn3;
    private int m_PlaybackRate = 50;
    private int m_PatternLoop = 0;
    private int m_Loops = -1;

    void Start()
    {
        psg.AddIrqCallback(m_PlaybackRate, OnIrqCallback);
        psg.AddIrqCallback(Instruments.InstrumentInstance.SAMPLE_RATE, OnSampleCallback);
        mute = new bool [ data.channels ];
        m_Instruments = new Instruments.InstrumentInstance [ data.channels ];
        m_PrevInstruments = new Instruments.InstrumentInstance[data.channels];
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
                        data.currentPattern = m_PatternLoop;
                }

                m_LastLineTick = Time.time;
            }
        }
    }

    public void OnSampleCallback()
    {
        if (!m_IsPlaying)
            return;

        for (int i = 0; i < data.channels; i++)
        {
            m_Instruments[i].UpdatePSGSample(psg, i);
        }
    }

    public void OnIrqCallback()
    {
        if (!m_IsPlaying)
            return;

        m_Counter++;
        if(m_Counter >= playbackSpeed)
        {
            if ( m_IsStopping ) {
                m_IsPlaying = false;
                m_IsStopping = true;
                return;
            }

            m_Counter = 0;
            for (int i = 0; i < data.channels; i++)
            {
                if ( mute [ i ] ) {
                    m_Instruments [ i ].note = VirtualKeyboard.Note.NoteOff;
                    continue;
                }

                m_PlayingPattern = m_CurrentPattern;
                SongData.ColumnEntry col = data.GetCurrentLine(m_CurrentPattern, i);

                int volume = col.data [ m_CurrentLine, 2 ];
                if ( volume >= 0 ) {
                    m_Instruments [ i ].relativeVolume = volume;
                }

                if (col.data[m_CurrentLine, 0] > 0)
                {
                    VirtualKeyboard.Note note = VirtualKeyboard.GetNote ( col.data [ m_CurrentLine, 0 ] );
                    if ( note == VirtualKeyboard.Note.NoteOff ) {
                        m_Instruments [ i ].note = VirtualKeyboard.Note.NoteOff;
                        psg.SetAttenuation ( i, 0 );
                    } else {
                        m_PrevInstruments[i] = m_Instruments[i];
                        m_Instruments [ i ] = instruments.presets [ col.data [ m_CurrentLine, 1 ] ];
                        m_Instruments [ i ].relativeVolume = volume >= 0 ? volume : 0xF;
                        m_Instruments[i].note = note;
                        m_Instruments[i].octave = VirtualKeyboard.GetOctave(col.data[m_CurrentLine, 0]);
                    } 
                }


                int fxVal = col.data [ m_CurrentLine, 4 ];

                if ( fxVal >= 0 ) {
                    switch ( col.data [ m_CurrentLine, 3 ] ) {
                        //arpreggio
                        case 0x00:
                            if ( fxVal == 0 ) {
                                m_Instruments [ i ].arpeggio = new int [ 0 ];
                            } else {
                                int hiArp, loArp;
                                SplitByte ( fxVal, out hiArp, out loArp );
                                m_Instruments [ i ].arpeggio = new int [ 3 ] { 0, loArp, hiArp };
                            }
                            break;

                        case 0x01:
                            m_Instruments [ i ].portamentoSpeed = fxVal;
                            break;

                        case 0x02:
                            m_Instruments [ i ].portamentoSpeed = -fxVal;
                            break;

                        case 0x03:
                            if ( m_Instruments [ i ].samplePlayback )
                                m_Instruments [ i ].pulseWidth = m_PrevInstruments [ i ].pulseWidth;
                            m_Instruments [ i ].SetAutoPortamento (m_PrevInstruments[i], fxVal);
                            break;

                        case 0x04:
                            int speed, depth;
                            SplitByte ( fxVal, out depth, out speed );
                            m_Instruments [ i ].vibratoDepth = depth;
                            m_Instruments [ i ].vibratoSpeed = speed;
                            break;

                        case 0x08:
                            int left, right;
                            SplitByte ( fxVal, out right, out left );
                            psg.SetStereo ( i, left != 0, right != 0 );
                            break;

                        case 0x0B:
                            //loop point
                            break;

                        case 0x0F:
                            playbackSpeed = fxVal;
                            break;

                        case 0x20:
                            int mode, fb;
                            SplitByte ( fxVal, out mode, out fb );
                            Instruments.InstrumentInstance.NOISE_FB = fb > 0;
                            Instruments.InstrumentInstance.NOISE_CHN3 = mode > 0;
                            break;

                        case 0x40:
                            m_Instruments [ i ].pulseWidthPanSpeed = fxVal;
                            break;

                        case 0xFF:
                            psg.PSGDirectWrite(fxVal);
                            break;
                    }
                }
            }


            m_CurrentLine++;
            m_MoveLine++;
            if(m_CurrentLine >= data.patternLength)
            {
                m_CurrentLine = 0;
                m_CurrentPattern++;

                if (m_CurrentPattern >= data.numPatterns)
                {
                    m_CurrentPattern = m_PatternLoop;
                    if ( m_Loops >= 0 ) {
                        m_IsStopping = m_Loops == 0;
                        m_Loops--;
                    }
                }
            }
        }

        for ( int i = 0 ; i < data.channels ; i++ ) {
            m_Instruments [ i ].UpdatePSG ( psg, i );
        }
    }

    public static void SplitByte(int val, out int b1, out int b2) {
        b1 = val & 0xF;
        b2 = ( val >> 4 ) & 0xF;
    }

    public void Play(int loops = -1)
    {
        m_PatternLoop = data.FindLoopPoint ( );
        m_StartSample = psg.currentSample;
        m_CurrentPattern = data.currentPattern;
        m_CurrentLine = 0;
        m_Counter = 10000;
        view.MoveLine ( -view.currentLine );
        m_LastLineTick = Time.time;
        m_IsPlaying = true;
        m_IsStopping = false;
        m_Loops = loops;
    }

    public void Stop()
    {
        m_IsPlaying = false;
        m_Instruments = new Instruments.InstrumentInstance [ data.channels ];
        psg.ResetChip();
    }
}
