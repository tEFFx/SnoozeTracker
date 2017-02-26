using UnityEngine;
using System.Collections;

public class VirtualKeyboard : MonoBehaviour {
    public enum Note { None = 0, C, Cs, D, Ds, E, F, Fs, G, Gs, A, As, B, NoteOff }

    public static readonly string[] NOTE_NAMES = { "--", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B", "OFF" };

    [System.Serializable]
    public class NoteKey
    {
        public Note note;
        public KeyCode key;
        public int octaveOffset;

        public bool GetNoteDown(int octave)
        {
            if (Input.GetKeyDown(key))
            {
                return true;
            }

            return false;
        }

        public bool GetNoteUp()
        {
            return Input.GetKeyUp(key);
        }
    }

    public int currentOctave
    {
        get { return m_CurrentOctave; }
        set
        {
            Mute();
            m_CurrentOctave = value;
        }
    }

    public History history;
    public PSGWrapper psg;
    public SongPlayback playback;
    public PatternView patternView;
    public Instruments instruments;
    public int currentInstrument;
    public int patternAdd = 1;
    public NoteKey[] noteBinds;
    public bool recording;

    private int m_CurrentOctave = 3;

    private Instruments.InstrumentInstance[] m_Instruments;

    void Awake() {
        psg.AddIrqCallback ( 50, OnIrqCallback );
        psg.AddIrqCallback(Instruments.InstrumentInstance.SAMPLE_RATE, OnSampleCallback);

        m_Instruments = new Instruments.InstrumentInstance[playback.data.channels];
    }

    void Update()
    {
        if ( Input.GetKeyDown ( KeyCode.Space ) ) {
            recording = !recording;
        }

        int sel = patternView.selection;
        if (sel % SongData.SONG_DATA_COUNT != 0)
            return;

        if (!Input.GetKey(KeyCode.LeftControl))
        {
            for (int i = 0; i < noteBinds.Length; i++)
            {
                if (noteBinds[i].GetNoteDown(m_CurrentOctave))
                {
                    if (noteBinds[i].note != Note.None && noteBinds[i].note != Note.NoteOff)
                    {
                        SetNoteOn ( noteBinds [ i ].note, m_CurrentOctave + noteBinds [ i ].octaveOffset );
                    }
                }
            }
        }

        for ( int i = 0 ; i < noteBinds.Length ; i++ ) {
            if ( noteBinds [ i ].GetNoteUp ( ) ) {
                SetNoteOff ( noteBinds [ i ].note, m_CurrentOctave + noteBinds [ i ].octaveOffset );
            }
        }
    }

    public void SetNoteOff(Note note, int octave) {
        int sel = patternView.selection;
        if ( sel % SongData.SONG_DATA_COUNT != 0 )
            return;

        if ( playback.isPlaying ) {
            //patternView.data [ sel ] = EncodeNoteInfo ( (int)Note.NoteOff, 0 );
            return;
        }

        for ( int i = 0 ; i < m_Instruments.Length ; i++ ) {
            if ( m_Instruments [ i ].note == note && m_Instruments [ i ].octave == octave ) {
                m_Instruments [ i ].note = Note.NoteOff;
            }
        }
    }

    public void SetNoteOn(Note note, int octave, int velocity = 0xF) {
        int sel = patternView.selection;
        if ( sel % SongData.SONG_DATA_COUNT != 0 )
            return;

        if ( recording ) {
            byte noteData = EncodeNoteInfo ( ( int ) note, octave );
            history.AddHistoryEntry ( patternView.selectedChannel );

            patternView.data [ sel ] = noteData;
            patternView.data [ sel + 1 ] = ( byte ) currentInstrument;
            if(velocity != 0xF)
                patternView.data [ sel + 2 ] = velocity;
            if(!playback.isPlaying)
                patternView.MoveLine ( patternAdd );
        }

        if ( playback.isPlaying )
            return;

        for ( int i = 0 ; i < m_Instruments.Length ; i++ ) {
            if ( m_Instruments [ i ].note == Note.None || m_Instruments [ i ].note == Note.NoteOff ) {
                m_Instruments [ i ] = instruments.presets [ currentInstrument ];
                m_Instruments [ i ].note = note;
                m_Instruments [ i ].octave = octave;
                m_Instruments [ i ].relativeVolume = velocity;
                break;
            }
        }
    }

    private void OnIrqCallback() {
        if ( playback.isPlaying )
            return;

        for (int i = 0; i < m_Instruments.Length; i++)
        {
            int chn = patternView.selectedChannel == 3 ? 3 : i;
            m_Instruments [i].UpdatePSG(psg, chn);

            if ( chn == 3 )
                break;
        }
    }

    private void OnSampleCallback()
    {
        if ( playback.isPlaying )
            return;

        for (int i = 0; i < m_Instruments.Length; i++)
        {
            int chn = patternView.selectedChannel == 3 ? 3 : i;
            m_Instruments [ i ].UpdatePSGSample ( psg, chn );

            if ( chn == 3 )
                break;
        }
    }

    public void Mute()
    {
        for (int i = 0; i < m_Instruments.Length; i++)
        {
            m_Instruments[i].note = Note.NoteOff;
        }
    }

    public static byte EncodeNoteInfo(int note, int octave)
    {
        int result = octave << 4;
        result |= note;
        return (byte)result;
    }

    public static Note GetNote(int noteData)
    {
        return (Note)(noteData & 0x0F);
    }

    public static int GetOctave(int noteData)
    {
        return (noteData >> 4) & 0x0F;
    }

	public static string FormatNote(int noteData)
    {
        int note = noteData & 0x0F;
        string result = NOTE_NAMES[note];

        if ( note == 0 || note == 13 )
            return result;

        int octave = ( noteData >> 4 ) & 0x0F;
        return result + octave.ToString ( );
    }
}
