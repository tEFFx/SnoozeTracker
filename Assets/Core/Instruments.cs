using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;

public class Instruments : MonoBehaviour {

    [Serializable]
    public struct InstrumentInstance : ISerializable {

        public InstrumentInstance(SerializationInfo info, StreamingContext context) {
            relativeVolume = 0xF;
            note = VirtualKeyboard.Note.None;
            m_SampleFreq = m_SampleTimer = 0;
            noteOffset = noteDelay = waveTableSampleRate = vibratoDepth = vibratoSpeed = octave = portamentoSpeed = m_LastSample = m_IrqTimer = m_PortamentoTimer = m_VolumeOffset = m_PWMTimer = m_PWM = 0;
            samplePlayback = m_AutoPortamento = m_UpdatedFrequency = m_PWMDir = m_PWMFlipFlop = false;
            volumeTable = new int [ ] { 0xF, 0xE, 0xD, 0xC };
            arpeggio = new int [ ] { 0x0 };
            pulseWidthMin = 25;
            pulseWidthMax = 75;
            pulseWidthPanSpeed = 1;
            customWaveform = Wave.Pulse;
            waveTable = new int [ 0 ];
            loopSample = false;
            name = string.Empty;

            sampleRelNote = 48; //C4

            foreach (SerializationEntry e in info) {
                switch ( e.Name ) {
                    case "vol": volumeTable = ( int [ ] ) e.Value; break;
                    case "arp": arpeggio = ( int [ ] ) e.Value; break;
                    case "vd": vibratoDepth = ( int ) e.Value; break;
                    case "vs": vibratoSpeed = ( int ) e.Value; break;
                    case "sp": samplePlayback = ( bool ) e.Value; break;
                    case "wav": customWaveform = (Wave)e.Value; break;
                    case "wt": waveTable = ( int [ ] ) e.Value; break;
                    case "wl": loopSample = ( bool ) e.Value; break;
                    case "wsr": waveTableSampleRate = ( int ) e.Value; break;
                    case "srn": sampleRelNote = ( int ) e.Value; break;
                    case "pmi": pulseWidthMin = ( int ) e.Value; break;
                    case "pma": pulseWidthMax = ( int ) e.Value; break;
                    case "ps": pulseWidthPanSpeed = ( int ) e.Value; break;
                    case "insname": name = ( string ) e.Value; break;
                }
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue ( "vol", volumeTable );
            info.AddValue ( "arp", arpeggio );
            info.AddValue ( "vd", vibratoDepth );
            info.AddValue ( "vs", vibratoSpeed );
            info.AddValue ( "sp", samplePlayback );
            info.AddValue ( "wav", customWaveform );
            info.AddValue ( "wt", waveTable );
            info.AddValue ( "wl", loopSample );
            info.AddValue ( "wsr", waveTableSampleRate );
            info.AddValue ( "srn", sampleRelNote );
            info.AddValue ( "pmi", pulseWidthMin );
            info.AddValue ( "pma", pulseWidthMax );
            info.AddValue ( "ps", pulseWidthPanSpeed );
            info.AddValue ( "insname", name );
        }

        public enum Wave { Pulse, Saw, Triangle, Sample, Sine }

        public static readonly int SAMPLE_RATE = 44100;
        public static readonly int PWM_STEPS = 100;
        public static readonly int PWMSPEED_MAX = 10;
        public static bool NOISE_FB = true;
        public static bool NOISE_CHN3 = false;
        public static readonly int LINEAR_STEPS = 0xF;
        public static readonly int[] LINEAR_VOLUME_TABLE = { 0xF, 0xF, 0xE, 0xE, 0xE, 0xD, 0xD, 0xC, 0xC, 0xB, 0xA, 0x9, 0x8, 0x6, 0x3, 0x0};

        public bool updatesFrequency {
            get {
                if ( arpeggio == null )
                    return false;
                return arpeggio.Length > 1 || portamentoSpeed != 0 || m_AutoPortamento || (vibratoDepth > 0 && vibratoSpeed > 0);
            }
        }

        public int pulseWidth { get { return m_PWM; } set { m_PWM = value; } }
        
        //not serialized
        public int portamentoSpeed;
        public int relativeVolume;
        public VirtualKeyboard.Note note;
        public int octave;

        //serialized
        public string name;         //added in 0.2
        public int[] volumeTable;
        public int[] waveTable;
        public int[] arpeggio;
        public int vibratoDepth;
        public int vibratoSpeed;
        public bool samplePlayback;
        public int pulseWidthMin;
        public int pulseWidthMax;
        public int pulseWidthPanSpeed;
        public Wave customWaveform;
        public bool loopSample;
        public int waveTableSampleRate;
        public int noteDelay;
        public int noteOffset;
        public int sampleRelNote;

        //not serialized
        private int m_IrqTimer, m_PortamentoTimer, m_VolumeOffset, m_PWMTimer, m_PWM, m_LastSample;
        private float m_SampleTimer, m_SampleFreq;
        private bool m_AutoPortamento, m_UpdatedFrequency, m_PWMDir, m_PWMFlipFlop;

        public void SetAutoPortamento(InstrumentInstance prev, int speed) {
            if (speed == 0 || prev.note == VirtualKeyboard.Note.None || prev.note == VirtualKeyboard.Note.NoteOff)
                return;

            float prevFreq = PSGWrapper.CalculateNoteFreq((int)prev.note, prev.octave);
            float currFreq = PSGWrapper.CalculateNoteFreq ( (int)note, octave);
            int relFreq = (int)(prevFreq - currFreq);

            m_PortamentoTimer = System.Math.Abs(relFreq) / speed;
            portamentoSpeed = speed * System.Math.Sign(relFreq);
            m_AutoPortamento = true;
        }

        public void UpdatePSG(PSGWrapper psg, int chn)
        {
            if ( note == VirtualKeyboard.Note.None || note == VirtualKeyboard.Note.NoteOff)
            {
                psg.SetAttenuation(chn, 0);
                return;
            }

            if ( !samplePlayback )
                psg.SetAttenuation ( chn, GetCurrentVol ( ) );

            if (!m_UpdatedFrequency || (updatesFrequency && !samplePlayback))
            {
                if (chn < 3)
                {
                    if (!samplePlayback)
                        psg.SetNote(chn, (int)note + GetNoteOffset(), octave, GetFreqOffset());
                    else
                        psg.SetFrequency(chn, 1);
                }
                else
                {
                    if ( samplePlayback ) {
                        psg.PSGDirectWrite ( 0xE2 );
                    }
                    else if (!NOISE_CHN3)
                    {
                        int cmd = 0xE0 | ((NOISE_FB ? 4 : 0) ) | (((int)note - 1) % 3);
                        //Debug.Log ( System.Convert.ToString ( cmd, 2 ) );
                        psg.PSGDirectWrite ( cmd );
                    }
                    else
                    {
                        psg.PSGDirectWrite(NOISE_FB ? 0xE7 : 0xE3);
                        psg.SetNote(2, (int)note + GetNoteOffset(), octave, GetFreqOffset());
                    }
                }
            }

            ClockInstrument();
            m_UpdatedFrequency = true;
        }

        public void UpdatePSGSample(PSGWrapper psg, int chn)
        {
            if ( !samplePlayback )
                return;

            if (GetCurrentVol() == 0)
                return;

            if (note == VirtualKeyboard.Note.None || note == VirtualKeyboard.Note.NoteOff)
            {
                psg.SetAttenuation(chn, 0);
                return;
            }

            if ( m_SampleTimer <= 0 ) {
                m_SampleFreq = ( PSGWrapper.CalculateNoteFreq ( ( int ) note + GetNoteOffset ( ), octave ) + GetFreqOffset ( ) );
                m_SampleTimer = SAMPLE_RATE / m_SampleFreq;
            }

            float divider = SAMPLE_RATE / m_SampleFreq;
            float phase = ( m_SampleTimer % divider ) / divider;
            float attn = 0;
            int smp = 0;

            switch (customWaveform) {
                case Wave.Pulse:
                    attn = phase < ((float)m_PWM / (float)PWM_STEPS) ? 0 : LINEAR_STEPS;
                    smp = ( int ) attn;
                    break;

                case Wave.Saw:
                    attn = Mathf.Ceil( phase * LINEAR_STEPS );
                    smp = ( int ) attn;
                    break;

                case Wave.Triangle:
                    attn = phase * LINEAR_STEPS * 2;
                    attn = Mathf.Ceil(Mathf.Abs(attn - LINEAR_STEPS ) );
                    smp = ( int ) attn;
                    break;

                case Wave.Sample:
                    if ( waveTable != null ) {
                        float noteOffset = ( PSGWrapper.CalculateNoteFreq ( ( int ) note + GetNoteOffset ( ), octave ) + GetFreqOffset ( ) ) / PSGWrapper.CalculateNoteFreq ( sampleRelNote + 1, 0 );
                        divider = SAMPLE_RATE / (waveTableSampleRate * noteOffset);
                        float pos = divider - m_SampleTimer;
                        int sampleIndex = ( int ) ( (m_SampleTimer / divider) % waveTable.Length );
                        if ( loopSample || ( m_SampleTimer / divider ) < waveTable.Length )
                            smp = waveTable [ sampleIndex ];
                        else
                            smp = LINEAR_STEPS;
                    }
                    break;

                case Wave.Sine:
                    attn = ( 1f + Mathf.Sin ( 2 * Mathf.PI * m_SampleFreq * m_SampleTimer / SAMPLE_RATE ) ) * 0.5f * LINEAR_STEPS;
                    smp = ( int ) attn;
                    break;
            }

            if ( customWaveform == Wave.Sample )
                m_SampleTimer++;
            else
                m_SampleTimer--;

            if ( m_LastSample != LINEAR_VOLUME_TABLE [ smp ] ) {
                attn = Math.Max ( 0, LINEAR_VOLUME_TABLE [ smp ] - ( 0xF - GetCurrentVol ( ) ) );
                psg.SetAttenuation ( chn, ( int ) attn );
            }
            m_LastSample = LINEAR_VOLUME_TABLE [ smp ];
        }

        private int GetNoteOffset()
        {
            if (arpeggio.Length == 0)
                return 0;

            return arpeggio[m_IrqTimer % arpeggio.Length] + noteOffset;
        }

        private int GetFreqOffset()
        {
            int vibrato = Mathf.RoundToInt(Mathf.Sin(m_IrqTimer * 0.1f * vibratoSpeed) * vibratoDepth);

            if (m_AutoPortamento && m_PortamentoTimer == 0)
            {
                m_AutoPortamento = false;
                portamentoSpeed = 0;
            }

            return m_PortamentoTimer * portamentoSpeed + vibrato;
        }

        public int GetCurrentVol()
        {
            if (volumeTable == null)
                return 0;
            return Math.Max(0, volumeTable[m_VolumeOffset] - (0xF - relativeVolume));
        }

        private void ClockInstrument()
        {
            if (volumeTable == null || m_VolumeOffset < volumeTable.Length - 1)
                m_VolumeOffset++;

            if (!m_AutoPortamento && portamentoSpeed != 0)
                m_PortamentoTimer++;
            else if (m_PortamentoTimer > 0)
                m_PortamentoTimer--;

            m_IrqTimer++;

            if(pulseWidthPanSpeed == 0)
                m_PWM = pulseWidthMin;
            else {
                m_PWM += m_PWMDir ? -pulseWidthPanSpeed : pulseWidthPanSpeed;
                if ( m_PWM > pulseWidthMax ) {
                    m_PWM = pulseWidthMax;
                    m_PWMDir = true;
                } else if ( m_PWM < pulseWidthMin ) {
                    m_PWM = pulseWidthMin;
                    m_PWMDir = false;
                }
            }
        }
    }

    public List<InstrumentInstance> presets = new List<InstrumentInstance>();

    void Awake() {
        CreateInstrument ( );
    }

    public void CreateInstrument() {
        InstrumentInstance created = new InstrumentInstance ( );
        created.volumeTable = new int [ ] { 0xF, 0xE, 0xD, 0xC };
        created.arpeggio = new int [ ] { 0x0 };
        created.pulseWidthMin = 25;
        created.pulseWidthMax = 75;
        created.sampleRelNote = 48;
        created.name = "Instrument " + presets.Count;
        presets.Add ( created );
    }
    
    public void CopyInstrument(int copyIndex) {
        presets.Add ( presets [ copyIndex ] );
    }

    public void RemoveInstrument(int removeIndex) {
        presets.RemoveAt ( removeIndex );
    }
}
