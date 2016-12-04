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
                int result = (octave + octaveOffset) << 4;
                result |= (int)note;
                noteData = (byte)result;
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

    public PSGWrapper psg;
    public PatternView patternView;
    public Instruments instruments;
    public int currentOctave = 3;
    public int currentInstrument;
    public int patternAdd = 1;
    public NoteKey[] noteBinds;

    private Instruments.InstrumentInstance m_Instrument;
    private Note m_PlayingNote;
    private int m_PlayingOctave;
    private bool m_Pressed;

    void Awake() {
        psg.AddIrqCallback ( 50, OnIrqCallback );
    }

    void Update()
    {
        int sel = patternView.selection;
        if (sel % SongData.SONG_DATA_COUNT != 0)
            return;

        for (int i = 0; i < noteBinds.Length; i++)
        {
            byte noteData;
            if (noteBinds[i].GetNoteDown(currentOctave, out noteData))
            {
                patternView.data[sel] = noteData;
                patternView.MoveLine(patternAdd);

                if (noteBinds[i].note != Note.None && noteBinds[i].note != Note.NoteOff)
                {
                    m_PlayingNote = noteBinds [ i ].note;
                    m_PlayingOctave = currentOctave + noteBinds [ i ].octaveOffset;
                    m_Instrument = instruments.presets [ currentInstrument ];
                    m_Pressed = true;
                    psg.SetFrequency ( patternView.selectedChannel, ( int ) m_PlayingNote, m_PlayingOctave );
                }
            }
            else if (noteBinds[i].GetNoteUp())
            {
                psg.SetAttenuation(patternView.selectedChannel, 0x0);
                m_Pressed = false;
            }
        }
    }

    private void OnIrqCallback() {
        if ( !m_Pressed )
            return;

        m_Instrument.Clock ( );
        psg.SetAttenuation ( patternView.selectedChannel, m_Instrument.GetCurrentVol());

        if(m_Instrument.updatesFrequency)
            psg.SetFrequency ( patternView.selectedChannel, (int)m_PlayingNote + m_Instrument.GetNoteOffset(), m_PlayingOctave );
    }

    public static Note GetNote(byte noteData)
    {
        return (Note)(noteData & 0x0F);
    }

    public static int GetOctave(byte noteData)
    {
        return (noteData >> 4) & 0x0F;
    }

	public static string FormatNote(byte noteData)
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
