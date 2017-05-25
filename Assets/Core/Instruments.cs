using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime.Serialization;

public class Instruments : MonoBehaviour {

    [Serializable]
    public struct InstrumentInstance : ISerializable {

        public InstrumentInstance(SerializationInfo info, StreamingContext context) {
            relativeVolume = 0xF;
            note = VirtualKeyboard.Note.None;
            m_SampleFreq = m_SampleTimer = 0;
            m_NoiseCounter = noiseModeLoopPoint = noteOffset = noteDelay = waveTableSampleRate = vibratoDepth = vibratoSpeed = m_Octave = portamentoSpeed = m_LastSample = m_IrqTimer = m_PortamentoTimer = m_VolumeOffset = m_PWMTimer = m_PWM = 0;
            arpAbsolute = samplePlayback = m_AutoPortamento = m_UpdatedFrequency = m_PWMDir = m_PWMFlipFlop = false;
            volumeTable = new int [ ] { 0xF, 0xE, 0xD, 0xC };
            arpeggio = new int [ ] { 0x0 };
            noiseMode = new int [ ] { 0x01 };
            pulseWidthMin = 25;
            pulseWidthMax = 75;
            pulseWidthPanSpeed = 1;
            customWaveform = Wave.Pulse;
            waveTable = new int [ 0 ];
            loopSample = false;
            name = string.Empty;
            volumeLoopPoint = 0;
            arpLoopPoint = 0;
            m_ArpCounter = 0;

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
                    case "volloop": volumeLoopPoint = (int) e.Value; break;
                    case "arploop": arpLoopPoint = (int) e.Value; break;
                    case "arpabs": arpAbsolute = ( bool ) e.Value; break;
                    case "noisemode": noiseMode = ( int [ ] ) e.Value; break;
                    case "noiseloop": noiseModeLoopPoint = ( int ) e.Value; break;
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
            info.AddValue ( "volloop", volumeLoopPoint );
            info.AddValue ( "arploop", arpLoopPoint );
            info.AddValue ( "arpabs", arpAbsolute );
            info.AddValue ( "noisemode", noiseMode );
            info.AddValue ( "noiseloop", noiseModeLoopPoint );
            info.AddValue ( "insname", name );
        }

        public enum Wave { Pulse, Saw, Triangle, Sample, Sine }

        public static readonly int SAMPLE_RATE = 44100;
        public static readonly int PWM_STEPS = 100;
        public static readonly int PWMSPEED_MAX = 10;
        public static readonly int LINEAR_STEPS = 0xF;
        public static readonly int[] LINEAR_VOLUME_TABLE = { 0xF, 0xF, 0xE, 0xE, 0xE, 0xD, 0xD, 0xC, 0xC, 0xB, 0xA, 0x9, 0x8, 0x6, 0x3, 0x0};

        public bool updatesFrequency {
            get {
                if ( arpeggio == null )
                    return false;
                return m_ArpCounter < arpeggio.Length || portamentoSpeed != 0 || m_AutoPortamento || (vibratoDepth > 0 && vibratoSpeed > 0) || m_NoiseCounter < noiseMode.Length;
            }
        }

        public bool useAbsNote { get { return arpAbsolute && ( m_ArpCounter < arpeggio.Length - 1 || arpLoopPoint > 0 ); } }

        public int pulseWidth { get { return m_PWM; } set { m_PWM = value; } }
        
        //not serialized
        public int portamentoSpeed;
        public int relativeVolume;
        public VirtualKeyboard.Note note;
        public int octave { get { return useAbsNote ? 2 : m_Octave; } set { m_Octave = value; } }

        //serialized
        public string name;         //added in 0.2
        public int[] volumeTable;
        public int volumeLoopPoint;
        public int[] waveTable;
        public int[] arpeggio;
        public int arpLoopPoint;
        public bool arpAbsolute;
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
        public int[] noiseMode;
        public int noiseModeLoopPoint;

        //not serialized
        private int m_IrqTimer, m_PortamentoTimer, m_VolumeOffset, m_PWMTimer, m_PWM, m_LastSample, m_ArpCounter, m_Octave, m_NoiseCounter;
        private float m_SampleTimer, m_SampleFreq;
        private bool m_AutoPortamento, m_UpdatedFrequency, m_PWMDir, m_PWMFlipFlop;

        public InstrumentInstance GetDeepCopy() {
            InstrumentInstance ins = this;

            ins.volumeTable = new int [ volumeTable.Length ];
            ins.arpeggio = new int [ arpeggio.Length ];
            Array.Copy ( volumeTable, ins.volumeTable, volumeTable.Length );
            Array.Copy ( arpeggio, ins.arpeggio, arpeggio.Length );

            if ( waveTable != null ) {
                ins.waveTable = new int [ waveTable.Length ];
                Array.Copy ( waveTable, ins.waveTable, waveTable.Length );
            }

            return ins;
        }

        public void ResizeVolumeTable(int increment) {
            ResizeArray(ref volumeTable, increment);
        }

        public void ResizeArpTable(int increment) {
            ResizeArray(ref arpeggio, increment);
        }

        public void ResizeNoiseTable(int increment) {
            ResizeArray ( ref noiseMode, increment );
        }

        private void ResizeArray(ref int[] array, int increment) {
            if (array.Length + increment < 1)
                return;
            
            Array.Resize(ref array, array.Length + increment);
            if (increment > 0 && array.Length > 1)
                array[array.Length - 1] = array[array.Length - 2];
        }

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
                        psg.SetNote(chn, GetNoteOffset( ( int ) note ), octave, GetFreqOffset());
                    else
                        psg.SetFrequency(chn, 1);
                }
                else if(!samplePlayback)
                {
                    int chn2, fb;
                    chn2 = ( noiseMode [ m_NoiseCounter ] & 0x2 );
                    fb = ( noiseMode [ m_NoiseCounter ] & 0x1 );

                    if (chn2 == 0)
                    {
                        int cmd = 0xE0 | ((fb > 0 ? 4 : 0) ) | (( GetNoteOffset ( ( int ) note ) - 1) % 3);
                        //Debug.Log ( System.Convert.ToString ( cmd, 2 ) );
                        psg.PSGDirectWrite ( cmd );
                    }
                    else
                    {
                        psg.PSGDirectWrite(fb > 0 ? 0xE7 : 0xE3);
                        psg.SetNote(2, GetNoteOffset( ( int ) note ), octave, GetFreqOffset());
                    }
                }
            }

            ClockInstrument();
            m_UpdatedFrequency = true;
        }

        static bool noiseFlip;
        public void UpdatePSGSample(PSGWrapper psg, int chn)
        {
            if ( !samplePlayback )
                return;

            if (GetCurrentVol() == 0)
                return;

            if ( chn == 3 ) {
                psg.PSGDirectWrite ( noiseFlip ? 0xE2 : 0xE1 );
                noiseFlip = !noiseFlip;
            }

            if (note == VirtualKeyboard.Note.None || note == VirtualKeyboard.Note.NoteOff)
            {
                psg.SetAttenuation(chn, 0);
                return;
            }

            if ( m_SampleTimer <= 0 ) {
                m_SampleFreq = ( PSGWrapper.CalculateNoteFreq ( GetNoteOffset ( ( int ) note ), octave ) + GetFreqOffset ( ) );
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
                        float noteOffset = ( PSGWrapper.CalculateNoteFreq ( GetNoteOffset ((int)note), octave ) + GetFreqOffset ( ) ) / PSGWrapper.CalculateNoteFreq ( sampleRelNote + 1, 0 );
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

        private int GetNoteOffset(int noteVal)
        {
            if (arpeggio.Length == 0)
                return 0;

            if ( useAbsNote )
                return arpeggio [ m_ArpCounter ];
            else if ( m_ArpCounter > arpeggio.Length - 1 && arpLoopPoint == 0 )
                return noteVal + noteOffset;

            return noteVal + arpeggio[m_ArpCounter] + noteOffset;
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
            else if (volumeLoopPoint > 0)
                m_VolumeOffset = volumeTable.Length - volumeLoopPoint;

            if (m_ArpCounter < arpeggio.Length - 1)
                m_ArpCounter++;
            else if(arpLoopPoint > 0)
                m_ArpCounter = arpeggio.Length - arpLoopPoint;

            if ( m_NoiseCounter < noiseMode.Length - 1 )
                m_NoiseCounter++;
            else if ( noiseModeLoopPoint > 0 )
                m_NoiseCounter = noiseMode.Length - noiseModeLoopPoint;

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

    public InstrumentInstance[] presets = new InstrumentInstance[0];

    void Awake() {
        CreateInstrument ( );
    }

    public void CreateInstrument() {
        int index = presets.Length;
        Array.Resize ( ref presets, index + 1 );
        presets[index].volumeTable = new int [ ] { 0xF, 0xE, 0xD, 0xC };
        presets [ index ].arpeggio = new int [ ] { 0x0 };
        presets [ index ].noiseMode = new int [ ] { 0x01 };
        presets [ index ].pulseWidthMin = 25;
        presets [ index ].pulseWidthMax = 75;
        presets [ index ].sampleRelNote = 48;
        presets [ index ].name = "Instrument " + index;
    }

    public void RemoveInstrument(int removeIndex) {
        if (presets.Length <= 1)
            return;
        for (int i = removeIndex; i < presets.Length - 1; i++) {
            presets[i] = presets[i + 1];
        }
        Array.Resize(ref presets, presets.Length - 1);
    }

    public void CopyInstrument(int copyIndex) {
        int index = presets.Length;
        Array.Resize ( ref presets, index + 1 );
        presets[index] = presets[copyIndex].GetDeepCopy();
    }
    
    //public void CopyInstrument(int copyIndex) {
    //    presets.Add ( presets [ copyIndex ] );
    //}

    //public void RemoveInstrument(int removeIndex) {
    //    presets.RemoveAt ( removeIndex );
    //}
}
