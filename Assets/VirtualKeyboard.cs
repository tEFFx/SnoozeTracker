using UnityEngine;
using System.Collections;

public class VirtualKeyboard : MonoBehaviour {
    public enum Note { None = 0, C, Cs, D, Ds, E, F, Fs, G, Gs, A, As, B, NoteOff }

    [System.Serializable]
    public class NoteKey
    {
        public Note note;
        public KeyCode key;
        public int octaveOffset;

        public bool GetNoteDown(int octave, out byte noteData)
        {
            if (Input.GetKeyDown(key))
            {
                noteData = EncodeNoteInfo((int)note, octave + octaveOffset);
                return true;
            }

            noteData = 0;
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
                byte noteData;
                if (noteBinds[i].GetNoteDown(m_CurrentOctave, out noteData))
                {
                    if (recording)
                    {
                        history.AddHistoryEntry(patternView.selectedChannel);

                        patternView.data[sel] = noteData;
                        patternView.data[sel + 1] = (byte)currentInstrument;
                        patternView.MoveLine(patternAdd);
                    }

                    if (noteBinds[i].note != Note.None && noteBinds[i].note != Note.NoteOff)
                    {
                        for (int j = 0; j < m_Instruments.Length; j++)
                        {
                            if (m_Instruments[j].note == Note.None || m_Instruments[j].note == Note.NoteOff)
                            {
                                m_Instruments[j] = instruments.presets[currentInstrument];
                                m_Instruments[j].note = noteBinds[i].note;
                                m_Instruments[j].octave = m_CurrentOctave + noteBinds[i].octaveOffset;
                                m_Instruments[j].relativeVolume = 0xF;
                                break;
                            }
                        }
                    }
                }
            }
        }

        for ( int i = 0 ; i < noteBinds.Length ; i++ ) {
            if ( noteBinds [ i ].GetNoteUp ( ) ) {
                for (int j = 0; j < m_Instruments.Length; j++)
                {
                    if (m_Instruments[j].note == noteBinds[i].note && m_Instruments[j].octave == m_CurrentOctave + noteBinds[i].octaveOffset) {
                        m_Instruments[j].note = Note.NoteOff;
                    }
                }
            }
        }
    }

    private void OnIrqCallback() {
        if ( playback.isPlaying )
            return;

        for (int i = 0; i < m_Instruments.Length; i++)
        {
            m_Instruments[i].UpdatePSG(psg, i);
        }
    }

    private void OnSampleCallback()
    {
        if ( playback.isPlaying )
            return;

        for (int i = 0; i < m_Instruments.Length; i++)
        {
            m_Instruments[i].UpdatePSGSample(psg, i);
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
        int octave = (noteData >> 4) & 0x0F;
        string result = ((Note)note).ToString() + octave.ToString();

        if ((Note)note == Note.None)
            result = "--";
        else if ((Note)note == Note.NoteOff)
            result = "OFF";
        else
            result = result.Replace('s', '#');

        return result;
    }
}
