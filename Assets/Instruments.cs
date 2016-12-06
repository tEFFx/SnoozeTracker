using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Instruments : MonoBehaviour {

    [System.Serializable]
    public struct InstrumentInstance {
        public bool updatesFrequency {
            get {
                if ( arpreggio == null )
                    return false;
                return arpreggio.Length > 1 || portamentoSpeed != 0 || m_AutoPortamento;
            }
        }

        public int[] volumeTable;
        public int[] arpreggio;
        public int vibratoDepth;
        public int vibratoSpeed;
        public int portamentoSpeed;
        public int relativeVolume;

        private int m_VolumeOffset;
        private int m_ArpOffset;
        private int m_VibratoTimer;
        private int m_PortamentoTimer;
        private bool m_AutoPortamento;

        public void Clock() {
            if ( volumeTable == null || m_VolumeOffset < volumeTable.Length - 1 )
                m_VolumeOffset++;

            m_ArpOffset++;
            if ( arpreggio == null || m_ArpOffset == arpreggio.Length )
                m_ArpOffset = 0;

            if ( !m_AutoPortamento && portamentoSpeed != 0 )
                m_PortamentoTimer++;
            else if(m_PortamentoTimer > 0)
                m_PortamentoTimer--;
        }

        public void SetAutoPortamento(int baseFreq, int speed, int dir) {
            if ( speed == 0 )
                return;

            m_PortamentoTimer = System.Math.Abs(baseFreq) / speed;
            portamentoSpeed = speed * -dir;
            Debug.Log ( speed * -dir );
            Debug.Log ( m_PortamentoTimer );
            m_AutoPortamento = true;
        }

        public int GetCurrentVol() {
            if ( volumeTable == null )
                return 0;
            return volumeTable [ m_VolumeOffset ] - (0xF - relativeVolume);
        }

        public int GetNoteOffset() {
            if ( arpreggio.Length == 0 )
                return 0;

            return arpreggio [ m_ArpOffset ];
        }

        public int GetFreqOffset() {
            if(m_AutoPortamento && m_PortamentoTimer == 0 ) {
                m_AutoPortamento = false;
                portamentoSpeed = 0;
            }

            return m_PortamentoTimer * portamentoSpeed;
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
