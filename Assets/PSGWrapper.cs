using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PSGWrapper : MonoBehaviour {
    public class IrqCallback
    {
        public System.Action onCounterReset;

        private int m_Count;
        private int m_Divider;

        public IrqCallback(int sampleRate, int frequency, System.Action callback = null)
        {
            m_Divider = sampleRate / frequency;
            onCounterReset += callback;
        }

        public void Clock()
        {
            m_Count++;
            if(m_Count >= m_Divider)
            {
                m_Count = 0;
                if (onCounterReset != null)
                    onCounterReset();
            }
        }
    }

    public class RegisterWrite
    {
        public int data;
        public int wait;

        public RegisterWrite(int _data, int _wait)
        {
            data = _data;
            wait = _wait;
        }
    }

    public delegate void OnSampleRead(float[] data, int channels);
    public OnSampleRead onSampleReadCallback;

    public long currentSample { get { return m_CurrentSample; } }
    public SN76489 chip { get { return m_PSGChip; } }
    public AudioSource audioSource;
    [HideInInspector]
    public bool recordRegisters;

    private long m_CurrentSample;
    private SN76489 m_PSGChip;
    private List<IrqCallback> m_Callbacks = new List<IrqCallback>();
    private List<RegisterWrite> m_RegisterWrites = new List<RegisterWrite>();
    private int m_WriteWait;

    void Awake()
    {
        m_PSGChip = new SN76489(AudioSettings.outputSampleRate, (int)SN76489.Clock.PAL);
        Debug.Log ( AudioSettings.outputSampleRate );   
        Mute();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i+=channels)
        {
            for (int j = 0; j < m_Callbacks.Count; j++)
            {
                m_Callbacks[j].Clock();
            }

            float sample = m_PSGChip.Render();

            for (int j = 0; j < channels; j++)
            {
                data[i + j] = sample;
            }
        }

        if (onSampleReadCallback != null)
            onSampleReadCallback(data, channels);
    }

    public void ManualClock()
    {
        for (int j = 0; j < m_Callbacks.Count; j++)
        {
            m_Callbacks[j].Clock();
        }

        m_WriteWait++;
    }

    public void Mute()
    {
        for (int i = 0; i < 4; i++)
        {
            SetAttenuation(i, 0);
        }
    }

    public void AddIrqCallback(int frequency, System.Action callback)
    {
        Debug.Log("Added IRQ " + frequency + " (" + (AudioSettings.outputSampleRate / frequency) + ")");
        m_Callbacks.Add(new IrqCallback(AudioSettings.outputSampleRate, frequency, callback));
    }

    public void SetNote(int channel, int note, int octave, int fineTune = 0)
    {
        if (channel < 3)
        {
            int freq = CalculatePSGFreq(note, octave, fineTune);
            SetFrequency(channel, freq);
        }
    }

    public void SetFrequency(int channel, int frequency)
    {
        byte reg = (byte)((channel * 2) << 4);
        byte data = (byte)(0x80 | reg | (frequency & 0xF));
        m_PSGChip.Write(data);

        RegisterWritten(data);

        byte data1 = data;
        data = (byte)((frequency >> 4) & 0x3F);
        m_PSGChip.Write(data);

        RegisterWritten(data);
    }

    public void SetAttenuation(int channel, int attenuation)
    {
        attenuation = 0xF - (attenuation & 0xF);
        byte reg = (byte)((channel * 2 + 1) << 4);
        byte data = (byte)(0x80 | reg | (attenuation & 0x0F));
        m_PSGChip.Write(data);

        RegisterWritten(data);
    }

    public void PSGDirectWrite(int data)
    {
        m_PSGChip.Write(data);
        RegisterWritten(data);
    }

    public List<RegisterWrite> RecordRegisters(bool record = true)
    {
        if (record)
        {
            m_RegisterWrites.Clear();
        }

        recordRegisters = record;

        return m_RegisterWrites;
    }

    private void RegisterWritten(int data)
    {
        if (!recordRegisters)
            return;

        m_RegisterWrites.Add(new RegisterWrite(data, m_WriteWait));
        m_WriteWait = 0;
    }

    public static int CalculatePSGFreq(int note, int octave, int fineTune = 0)
    {
        int div = (int)SN76489.Clock.PAL / 32 / (int)(CalculateNoteFreq(note, octave) + fineTune);
        //Debug.Log(div.ToString("X2"));
        return div;
    }

    public static float CalculateNoteFreq(int note, int octave)
    {
        int relativeNote = (note + octave * 12) - 58;
        return 440 * Mathf.Pow(Mathf.Pow(2, 1f / 12f), relativeNote);
    }
}
