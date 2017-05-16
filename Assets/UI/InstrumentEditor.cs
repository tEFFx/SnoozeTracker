using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstrumentEditor : MonoBehaviour {
    public GameObject instrumentPrefab;

    public Instruments instruments;
    public VirtualKeyboard keyboard;

    private List<InstrumentButton> m_Instruments = new List<InstrumentButton>();
    private InstrumentButton m_SelectedInstrument;

	// Update is called once per frame
	void Update () {
        UpdateInstruments ( );
    }

    public void UpdateInstruments() {
        int currCount = m_Instruments.Count;
        int presets = instruments.presets.Count;

        if ( currCount == presets )
            return;

        if(currCount < presets) {
            int add = presets - currCount;
            Debug.Log ( "Adding " + add );
            for ( int i = 0 ; i < add ; i++ ) {
                GameObject obj = Instantiate ( instrumentPrefab, transform );
                InstrumentButton btnInstance = obj.GetComponent<InstrumentButton> ( );
                btnInstance.editor = this;
                m_Instruments.Add ( btnInstance );
            }
        } else {
            int remove = currCount - presets;
            Debug.Log ( "Removing " + remove + ", curr count " + m_Instruments.Count );
            for ( int i = 0 ; i < remove ; i++ ) {
                DestroyImmediate ( m_Instruments [ i ].gameObject );
            }

            m_Instruments.RemoveRange ( 0, remove );
            Debug.Log ( "Count after " + m_Instruments.Count );
        }

        UpdateInstrumentInfo ( );

        if ( m_SelectedInstrument == null )
            SetSelectedInstrument ( 0 );
    }

    public void UpdateInstrumentInfo() {
        for ( int i = 0 ; i < m_Instruments.Count ; i++ ) {
            m_Instruments [ i ].UpdateInfo ( );
        }
    }

    public void SetSelectedInstrument(int index) {
        if ( index == keyboard.currentInstrument && m_SelectedInstrument != null )
            return;

        if ( m_SelectedInstrument != null )
            m_SelectedInstrument.SetSelected ( false );

        m_SelectedInstrument = m_Instruments [ index ];
        m_SelectedInstrument.SetSelected ( true );

        keyboard.currentInstrument = index;
    }

    public void NewInstrument() {
        instruments.CreateInstrument ( );
    }

    public void RemoveInstrument() {
        instruments.RemoveInstrument ( m_SelectedInstrument.transform.GetSiblingIndex ( ) );
    }

    public void CopyInstrument() {
        instruments.CopyInstrument ( m_SelectedInstrument.transform.GetSiblingIndex ( ) );
    }
}
