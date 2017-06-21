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
            m_Count = m_Divider = sampleRate / frequency;
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

        bool firstCallback = false;
    }

    public class RegisterWrite
    {
        public FileManagement.VGMCommands command;
        public int data;
        public int wait;
        public bool end;
        public int pattern;

        public RegisterWrite(FileManagement.VGMCommands _command, int _wait, int _data, int _pattern, bool _end = false)
        {
            command = _command;
            data = _data;
            wait = _wait;
            end = _end;
            pattern = _pattern;
        }
    }

    public delegate void OnSampleRead(float[] data, int channels);
    public OnSampleRead onSampleReadCallback;

    public long currentSample { get { return m_CurrentSample; } }
    public SN76489 chip { get { return m_PSGChip; } }
    public AudioSource audioSource;
    public SongPlayback playback;
	public Arduino arduino;
    [HideInInspector]
    public bool recordRegisters;

    public SN76489.Clock clockFrequency = SN76489.Clock.PAL;
    public int refreshRate = 50;

    private long m_CurrentSample;
    private SN76489 m_PSGChip;
    private List<IrqCallback> m_Callbacks = new List<IrqCallback>();
    private List<RegisterWrite> m_RegisterWrites = new List<RegisterWrite>();

    private int m_WriteWait;
    private int m_SampleRate;

    void Awake()
    {
        m_SampleRate = AudioSettings.outputSampleRate;
        m_PSGChip = new SN76489(m_SampleRate, (int)clockFrequency);
        Debug.Log ( AudioSettings.outputSampleRate );   
        ResetChip();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        GetSamples(data, channels);
    }

    public void GetSamples(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            for (int j = 0; j < m_Callbacks.Count; j++)
            {
                m_Callbacks[j].Clock();
            }

            double left, right;
            m_PSGChip.Render(out left, out right);

            if (channels < 2)
            {
                data[i] = (float)(left + right) * 0.5f;
            }
            else
            {
                data[i] = (float)left;
                data[i + 1] = (float)right;
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

        if(recordRegisters)
            m_WriteWait++;
    }

    public void ResetChip()
    {
        m_PSGChip.Reset ( );

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
            int freq = CalculatePSGFreq((int)clockFrequency, note, octave, fineTune);
            freq = Mathf.Clamp ( freq, 0x00, 0x3FF );
            SetFrequency(channel, freq);
        }
    }

    public void SetFrequency(int channel, int frequency)
    {
        if ( chip.GetRegister ( channel * 2 ) == frequency )
            return;

        byte reg = (byte)((channel * 2) << 4);
        byte data = (byte)(0x80 | reg | (frequency & 0xF));
        m_PSGChip.Write(data);
		arduino.WriteByte((byte)data);

        RegisterWritten( FileManagement.VGMCommands.PSGWrite, data );

        byte data1 = data;
        data = (byte)((frequency >> 4) & 0x3F);
        m_PSGChip.Write(data);
		arduino.WriteByte((byte)data);
        RegisterWritten( FileManagement.VGMCommands.PSGWrite, data );
    }

    public void SetAttenuation(int channel, int attenuation)
    {
        attenuation = 0xF - ( attenuation & 0xF );
        if ( chip.GetRegister ( channel * 2 + 1 ) == attenuation )
            return;

        byte reg = (byte)((channel * 2 + 1) << 4);
        byte data = (byte)(0x80 | reg | (attenuation & 0x0F));
        m_PSGChip.Write(data);
		arduino.WriteByte((byte)data);
        RegisterWritten( FileManagement.VGMCommands.PSGWrite, data );
    }

    public void SetStereo(int channel, bool left, bool right) {
        m_PSGChip.SetStereo ( channel, left, right );
        RegisterWritten ( FileManagement.VGMCommands.StereoSet, m_PSGChip.stereoByte );
    }

    public void PSGDirectWrite(int data)
    {
        int reg = ( data >> 4 ) & 7;
        if ( ( data & 128 ) != 0 && chip.GetRegister ( reg ) == ( data & 0xF ) )
            return;

        m_PSGChip.Write(data);
        RegisterWritten( FileManagement.VGMCommands.PSGWrite, data );
		arduino.WriteByte((byte)data);
    }

    public List<RegisterWrite> RecordRegisters(bool record = true)
    {
        if (record)
        {
            m_RegisterWrites.Clear();
        }

        if ( recordRegisters && !record ) {
            m_RegisterWrites.Add ( new RegisterWrite (FileManagement.VGMCommands.EOF, m_WriteWait, 0, playback.currentPattern, true ) );
        }

        recordRegisters = record;

        return m_RegisterWrites;
    }

    private void RegisterWritten(FileManagement.VGMCommands command, int data)
    {
        if (!recordRegisters)
            return;

        m_RegisterWrites.Add(new RegisterWrite(command, m_WriteWait, data, playback.currentPattern));
        m_WriteWait = 0;
    }

    public static int CalculatePSGFreq(int clock, int note, int octave, int fineTune = 0)
    {
        int freq = ( int ) ( CalculateNoteFreq ( note, octave ) + fineTune );
        if ( freq == 0 )
            return 0;

        int div = clock / 32 / freq;
        //Debug.Log(div.ToString("X2"));
        return div;
    }

    public static float CalculateNoteFreq(int note, int octave)
    {
        int relativeNote = (note + octave * 12) - 58;
        return 440 * Mathf.Pow(Mathf.Pow(2, 1f / 12f), relativeNote);
    }
}
