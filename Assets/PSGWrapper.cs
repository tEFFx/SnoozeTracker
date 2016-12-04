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
            m_Divider = frequency / sampleRate;
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

    public long currentSample { get { return m_CurrentSample; } }
    public AudioSource audioSource;

    private long m_CurrentSample;
    private SN76489 m_PSGChip;
    private List<IrqCallback> m_Callbacks = new List<IrqCallback>();

    void Awake()
    {
        m_PSGChip = new SN76489(AudioSettings.outputSampleRate, (int)SN76489.Clock.PAL);
        Debug.Log ( AudioSettings.outputSampleRate );   
        Mute();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        //Debug.Log ( "Filter read! "+ data.Length * channels);

        for (int i = 0; i < data.Length; i+=channels)
        {
            float sample = m_PSGChip.Render();

            for (int j = 0; j < channels; j++)
            {
                data[i + j] = sample;
            }
        }

        for (int i = 0; i < m_Callbacks.Count; i++)
        {
            m_Callbacks[i].Clock();
        }
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
        m_Callbacks.Add(new IrqCallback(AudioSettings.outputSampleRate, frequency, callback));
    }

    public void SetFrequency(int channel, int note, int octave, int fineTune = 0)
    {
        if (channel < 3)
        {
            int freq = CalculateFrequency(note, octave) + fineTune;
            byte reg = (byte)((channel * 2) << 4);
            byte data = (byte)(0x80 | reg | (freq & 0xF));
            m_PSGChip.Write(data);
            byte data1 = data;
            data = (byte)((freq >> 4) & 0x3F);
            m_PSGChip.Write(data);

            //Debug.Log("Data sent: " + System.Convert.ToString(data1, 2) + ", " + System.Convert.ToString(data, 2) + ". Should be " + System.Convert.ToString(freq, 2) + " (" + freq.ToString("X2") + ")");
        }
    }

    public void SetAttenuation(int channel, int attenuation)
    {
        attenuation = 0xF - (attenuation & 0xF);
        byte reg = (byte)((channel * 2 + 1) << 4);
        byte data = (byte)(0x80 | reg | (attenuation & 0x0F));
        m_PSGChip.Write(data);
    }

    public static int CalculateFrequency(int note, int octave)
    {
        int relativeNote = (note + octave * 12) - 58;
        float freq = 440 * Mathf.Pow(Mathf.Pow(2, 1f / 12f), relativeNote);
        int div = (int)SN76489.Clock.PAL / 32 / (int)freq;
        //Debug.Log(div.ToString("X2"));
        return div;
    }
}
