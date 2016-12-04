using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Instruments : MonoBehaviour {

    [System.Serializable]
    public struct InstrumentInstance {
        public int[] volumeTable;
        public int arpreggio;
        public int vibrato;

        private int m_VolumeOffset;

        public void Clock() {
            if ( m_VolumeOffset < volumeTable.Length - 1 )
                m_VolumeOffset++;
        }

        public int GetCurrentVol() {
            return volumeTable [ m_VolumeOffset ];
        }
    }

    public List<InstrumentInstance> presets = new List<InstrumentInstance>();
    
}
