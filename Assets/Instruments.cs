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
            m_SampleTimer = 0;
            pulseWidthPanSpeed = vibratoDepth = vibratoSpeed = octave = portamentoSpeed = m_IrqTimer = m_PortamentoTimer = m_VolumeOffset = m_PWMTimer = m_PWM = 0;
            samplePlayback = m_AutoPortamento = m_UpdatedFrequency = m_PWMDir = m_PWMFlipFlop = false;
            volumeTable = new int [ ] { 0xF, 0xE, 0xD, 0xC };
            arpeggio = new int [ ] { 0x0 };
            pulseWidthMin = 4;
            pulseWidthMax = 26;

            foreach (SerializationEntry e in info) {
                switch ( e.Name ) {
                    case "vol": volumeTable = ( int [ ] ) e.Value; break;
                    case "arp": arpeggio = ( int [ ] ) e.Value; break;
                    case "vd": vibratoDepth = ( int ) e.Value; break;
                    case "vs": vibratoSpeed = ( int ) e.Value; break;
                    case "sp": samplePlayback = ( bool ) e.Value; break;
                    case "pmi": pulseWidthMin = ( int ) e.Value; break;
                    case "pma": pulseWidthMax = ( int ) e.Value; break;
                    case "ps": pulseWidthPanSpeed = ( int ) e.Value; break;
                    default: Debug.LogWarning ( "Serialized field " + e.Name + " does not exist." ); break;
                }
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue ( "vol", volumeTable );
            info.AddValue ( "arp", arpeggio );
            info.AddValue ( "vd", vibratoDepth );
            info.AddValue ( "vs", vibratoSpeed );
            info.AddValue ( "sp", samplePlayback );
            info.AddValue ( "pmi", pulseWidthMin );
            info.AddValue ( "pma", pulseWidthMax );
            info.AddValue ( "ps", pulseWidthPanSpeed );
        }

        public static readonly int SAMPLE_RATE = 16000;
        public static readonly int PWM_STEPS = 32;
        public static bool m_NoiseFB = true;
        public static bool m_NoiseChn3 = false;

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

        public int[] volumeTable;
        public int[] arpeggio;
        public int vibratoDepth;
        public int vibratoSpeed;
        public bool samplePlayback;
        public int pulseWidthMin;
        public int pulseWidthMax;
        public int pulseWidthPanSpeed;

        private int m_IrqTimer, m_PortamentoTimer, m_VolumeOffset, m_PWMTimer, m_PWM;
        private float m_SampleTimer;
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
            if (note == VirtualKeyboard.Note.None || note == VirtualKeyboard.Note.NoteOff)
            {
                psg.SetAttenuation(chn, 0);
                return;
            }

            psg.SetAttenuation(chn, GetCurrentVol());

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
                    if (!m_NoiseChn3)
                    {
                        int cmd = 0xE0 | (((int)note - 1) % 3) | ((m_NoiseFB ? 1 : 0) << 2);
                        psg.chip.Write(cmd);
                    }
                    else
                    {
                        psg.chip.Write(0xE7);
                        psg.SetNote(2, (int)note + GetNoteOffset(), octave, GetFreqOffset());
                    }
                }
            }

            ClockInstrument();
            m_UpdatedFrequency = true;
        }

        public void UpdatePSGSample(PSGWrapper psg, int chn)
        {
            if ( !samplePlayback || chn == 3 )
                return;

            if ( note == VirtualKeyboard.Note.None || note == VirtualKeyboard.Note.NoteOff)
            {
                psg.SetAttenuation(chn, 0);
                return;
            }

            if ( m_SampleTimer <= 0 ) {
                m_SampleTimer += ( SAMPLE_RATE / (PSGWrapper.CalculateNoteFreq ( ( int ) note + GetNoteOffset ( ), octave ) + GetFreqOffset()) / PWM_STEPS );
                m_PWMTimer++;

                if ( m_PWMTimer >= ( m_PWMFlipFlop ? m_PWM : PWM_STEPS - m_PWM ) ) {
                    m_PWMFlipFlop = !m_PWMFlipFlop;
                    m_PWMTimer = 0;
                }
            }

            psg.SetAttenuation(chn, m_PWMFlipFlop ? GetCurrentVol() : 0);

            m_SampleTimer--;
        }

        private int GetNoteOffset()
        {
            if (arpeggio.Length == 0)
                return 0;

            return arpeggio[m_IrqTimer % arpeggio.Length];
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

        private int GetCurrentVol()
        {
            if (volumeTable == null)
                return 0;
            return volumeTable[m_VolumeOffset] - (0xF - relativeVolume);
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

            if ( pulseWidthPanSpeed == 0 || m_IrqTimer % pulseWidthPanSpeed == 0 ) {
                m_PWM += m_PWMDir ? -1 : 1;
                if ( m_PWM > pulseWidthMax ) {
                    m_PWMDir = true;
                }else if(m_PWM < pulseWidthMin ) {
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
        created.pulseWidthMin = 8;
        created.pulseWidthMax = 26;
        presets.Add ( created );
    }
    
}
