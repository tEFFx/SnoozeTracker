using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Instruments : MonoBehaviour {

    [System.Serializable]
    public struct InstrumentInstance {
        public bool updatesFrequency { get { return arpreggio.Length > 0; } }

        public int[] volumeTable;
        public int[] arpreggio;
        public int vibratoDepth;
        public int vibratoSpeed;
        public int relativeVolume;

        private int m_VolumeOffset;
        private int m_ArpOffset;
        private int m_VibratoTimer;

        public void Clock() {
            if ( m_VolumeOffset < volumeTable.Length - 1 )
                m_VolumeOffset++;

            m_ArpOffset++;
            if ( m_ArpOffset == arpreggio.Length )
                m_ArpOffset = 0;
        }

        public int GetCurrentVol() {
            return volumeTable [ m_VolumeOffset ];
        }

        public int GetNoteOffset() {
            if ( arpreggio.Length == 0 )
                return 0;

            return arpreggio [ m_ArpOffset ];
        }
    }

    public List<InstrumentInstance> presets = new List<InstrumentInstance>();
    
}
