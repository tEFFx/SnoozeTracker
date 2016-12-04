using UnityEngine;
using System.Collections;

public class SongPlayback : MonoBehaviour {
    public PSGWrapper psg;
    public SongData data;
    public int playbackSpeed;

    private int m_CurrentPattern;
    private int m_CurrentLine;
    private int m_Counter;
    private long m_StartSample;
    private bool m_IsPlaying;

    void Start()
    {
        psg.AddIrqCallback(50, OnIrqCallback);
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
                    psg.SetAttenuation(i, 0);
                }else if(note != VirtualKeyboard.Note.None)
                {
                    psg.SetFrequency(i, note, VirtualKeyboard.GetOctave(col.data[m_CurrentLine, 0]));
                    psg.SetAttenuation(i, 0xF);
                }
            }

            m_CurrentLine++;
            if(m_CurrentLine >= data.lines)
            {
                m_CurrentLine = 0;
                m_CurrentPattern++;
                if (m_CurrentPattern >= data.numPatterns)
                    m_CurrentPattern = 0;
            }
        }
    }

    public void Play()
    {
        m_StartSample = psg.currentSample;
        m_CurrentPattern = data.currentPattern;
        m_CurrentLine = 0;
        m_IsPlaying = true;
    }

    public void Stop()
    {
        m_IsPlaying = false;
        psg.Mute();
    }
}
