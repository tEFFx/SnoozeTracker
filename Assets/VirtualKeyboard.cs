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
    public int currentOctave = 3;
    public int patternAdd = 1;
    public NoteKey[] noteBinds;

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
                    psg.SetAttenuation(patternView.selectedChannel, 0xF);
                    psg.SetFrequency(patternView.selectedChannel, noteBinds[i].note, currentOctave + noteBinds[i].octaveOffset);
                }
            }
            else if (noteBinds[i].GetNoteUp())
            {
                psg.SetAttenuation(patternView.selectedChannel, 0x0);
            }
        }
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
