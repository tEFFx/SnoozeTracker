using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Instruments : MonoBehaviour {

    [System.Serializable]
    public struct InstrumentInstance {
        public static readonly int SAMPLE_RATE = 16000;
        public static bool m_NoiseFB = true;
        public static bool m_NoiseChn3 = false;

        public bool updatesFrequency {
            get {
                if ( arpreggio == null )
                    return false;
                return arpreggio.Length > 1 || portamentoSpeed != 0 || m_AutoPortamento || (vibratoDepth > 0 && vibratoSpeed > 0);
            }
        }

        public int[] volumeTable;
        public int[] arpreggio;
        public int vibratoDepth;
        public int vibratoSpeed;
        public int portamentoSpeed;
        public int relativeVolume;
        public VirtualKeyboard.Note note;
        public int octave;
        public bool samplePlayback;
        public int pulseWidth;

        private int m_VolumeOffset;
        private int m_ArpOffset;
        private int m_VibratoTimer;
        private int m_PortamentoTimer;
        private long m_SampleTimer;
        private bool m_AutoPortamento;
        private bool m_UpdatedFrequency;

        public void SetAutoPortamento(InstrumentInstance prev, int speed) {
            if (speed == 0 || prev.note == VirtualKeyboard.Note.None || prev.note == VirtualKeyboard.Note.NoteOff)
                return;

            int prevFreq = PSGWrapper.CalculatePSGFreq((int)prev.note, prev.octave);
            int currFreq = PSGWrapper.CalculatePSGFreq((int)note, octave);
            int relFreq = prevFreq - currFreq;

            m_PortamentoTimer = System.Math.Abs(relFreq) / speed;
            portamentoSpeed = speed * -System.Math.Abs(relFreq);
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

        bool flipFlop;
        public void UpdatePSGSample(PSGWrapper psg, int chn)
        {
            if (note == VirtualKeyboard.Note.None || note == VirtualKeyboard.Note.NoteOff)
            {
                psg.SetAttenuation(chn, 0);
                return;
            }

            if (!samplePlayback || chn == 3)
                return;

            int div = (int)(SAMPLE_RATE / PSGWrapper.CalculateNoteFeq((int)note, octave + 1));

            if (m_SampleTimer % div == 0)
                flipFlop = !flipFlop;

            psg.SetAttenuation(chn, flipFlop ? GetCurrentVol() : 0);

            m_SampleTimer++;
        }

        private int GetNoteOffset()
        {
            if (arpreggio.Length == 0)
                return 0;

            return arpreggio[m_ArpOffset];
        }

        private int GetFreqOffset()
        {
            int vibrato = Mathf.RoundToInt(Mathf.Sin(m_VibratoTimer * 0.1f * vibratoSpeed) * vibratoDepth);

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

            m_ArpOffset++;
            if (arpreggio == null || m_ArpOffset == arpreggio.Length)
                m_ArpOffset = 0;

            if (!m_AutoPortamento && portamentoSpeed != 0)
                m_PortamentoTimer++;
            else if (m_PortamentoTimer > 0)
                m_PortamentoTimer--;

            if (vibratoSpeed > 0 && vibratoDepth > 0)
                m_VibratoTimer++;
        }
    }

    public List<InstrumentInstance> presets = new List<InstrumentInstance>();

    void Awake() {
        CreateInstrument ( );
    }

    public void CreateInstrument() {
        InstrumentInstance created = new InstrumentInstance ( );
        created.volumeTable = new int [ ] { 0xF, 0xE, 0xD, 0xC };
        created.arpreggio = new int [ ] { 0x0 };
        presets.Add ( created );
    }
    
}
