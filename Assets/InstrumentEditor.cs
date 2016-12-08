using UnityEngine;
using System.Collections;

public class InstrumentEditor : MonoBehaviour {
    public Instruments.InstrumentInstance instrument { get { return instruments.presets [ keyboard.currentInstrument ]; } }

    public string volumeEnvelope {
        get {
            return m_VolumeEnvelope;
        }

        set {
            if ( m_VolumeEnvelope == value )
                return;

            Instruments.InstrumentInstance preset = instrument;
            m_VolumeEnvelope = value;
            StringToArray ( m_VolumeEnvelope, ref preset.volumeTable );
            instruments.presets [ keyboard.currentInstrument ] = preset;
        }
    }

    public string arpEnvelope {
        get { return m_Arpeggio; }
        set {
            if ( m_Arpeggio == value )
                return;
            Instruments.InstrumentInstance preset = instrument;
            m_Arpeggio = value;
            StringToArray ( m_Arpeggio, ref preset.arpeggio );
            instruments.presets [ keyboard.currentInstrument ] = preset;
        }
    }

    public Instruments instruments;
    public VirtualKeyboard keyboard;
    public Vector2 padding;
    public Vector2 size;

    private string m_VolumeEnvelope;
    private string m_Arpeggio;
    private bool m_HideFields;

    void Start() {
        UpdateAttributes ( );
    }

    void OnGUI() {

        if ( Event.current.keyCode == KeyCode.Tab ) {
            if ( Event.current.type == EventType.KeyDown )
                m_HideFields = true;
            else
                m_HideFields = false;
        }
        Rect rect = new Rect ( new Vector2 ( padding.x, padding.y ), size );

        GUILayout.BeginArea ( rect );

        GUILayout.BeginHorizontal ( );
        GUILayout.Box ( "Ins " + keyboard.currentInstrument );
        if ( GUILayout.Button ( "<" ) )
            IncInstrument ( -1 );
        if ( GUILayout.Button ( ">" ) )
            IncInstrument ( 1 );
        GUILayout.EndHorizontal ( );

        GUILayout.Box ( "Volume Envelope" );
        volumeEnvelope = TabSafeTextField ( volumeEnvelope );
        GUILayout.Box ( "Arpeggio" );
        arpEnvelope = TabSafeTextField ( arpEnvelope );

        bool samp = instruments.presets[keyboard.currentInstrument].samplePlayback;
        samp = GUILayout.Toggle(samp, "SID");

        if(samp != instruments.presets[keyboard.currentInstrument].samplePlayback)
        {
            Instruments.InstrumentInstance ins = instruments.presets[keyboard.currentInstrument];
            ins.samplePlayback = samp;
            instruments.presets[keyboard.currentInstrument] = ins;
        }

        GUILayout.EndArea ( );
    }

    string TabSafeTextField(string value) {
        GUI.enabled = !m_HideFields;
        string res = GUILayout.TextField ( value );
        GUI.enabled = true;
        return res;
    }


    public void UpdateAttributes() {
        m_VolumeEnvelope = ArrayToString ( instrument.volumeTable );
        m_Arpeggio = ArrayToString ( instrument.arpeggio );
    }

    private void IncInstrument(int dir) {
        keyboard.currentInstrument += dir;
        if ( keyboard.currentInstrument < 0 )
            keyboard.currentInstrument = 0;
        if ( keyboard.currentInstrument >= instruments.presets.Count )
            instruments.CreateInstrument ( );

        UpdateAttributes ( );
    }

    private string ArrayToString(int[] array) {
        if ( array == null )
            return "";
        string res = "";
        for ( int i = 0 ; i < array.Length ; i++ ) {
            res += array[i].ToString ( "X" ) + " ";
        }

        return res;
    }

    private void StringToArray(string str, ref int[] array) {
        string [ ] data = str.Split ( new char[]{ ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries );
        array = new int [ data.Length ];
        for ( int i = 0 ; i < data.Length ; i++ ) {
            int val;
            if ( int.TryParse ( data [ i ], System.Globalization.NumberStyles.HexNumber, null, out val ) )
                array [ i ] = val;
        }
    }
}
