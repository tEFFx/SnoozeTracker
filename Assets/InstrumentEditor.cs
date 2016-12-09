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
    public float volWidth;

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

        GUILayout.BeginVertical (GUILayout.Width(size.x - volWidth - 16));
        GUILayout.BeginHorizontal ( );
        GUILayout.Box ( "Ins " + keyboard.currentInstrument );
        if ( GUILayout.Button ( "<" ) )
            IncInstrument ( -1 );
        if ( GUILayout.Button ( ">" ) )
            IncInstrument ( 1 );
        GUILayout.EndHorizontal ( );
        GUILayout.Box ( "Arpeggio" );
        arpEnvelope = TabSafeTextField ( arpEnvelope );

        bool samp = instruments.presets [ keyboard.currentInstrument ].samplePlayback;
        samp = GUILayout.Toggle ( samp, "Custom waves" );

        if ( samp != instruments.presets [ keyboard.currentInstrument ].samplePlayback ) {
            Instruments.InstrumentInstance ins = instruments.presets [ keyboard.currentInstrument ];
            ins.samplePlayback = samp;
            instruments.presets [ keyboard.currentInstrument ] = ins;
        }

        if (samp)
        {
            GUILayout.BeginHorizontal();
            WaveButton(Instruments.InstrumentInstance.Wave.Pulse);
            WaveButton(Instruments.InstrumentInstance.Wave.Saw);
            WaveButton(Instruments.InstrumentInstance.Wave.Triangle);
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical ( );

        GUILayout.BeginVertical ( GUILayout.Width ( volWidth ) );
        GUILayout.Box ( "Volume Envelope" );
        ArraySlider ( instrument.volumeTable, 0, 0xF );

        GUILayout.FlexibleSpace ( );
        GUILayout.BeginHorizontal ( );
        GUILayout.Box ( instrument.volumeTable.Length.ToString ( "X2" ) );
        if ( GUILayout.Button ( "-" ) )
            ChangeVolTableSize ( -1 );
        if ( GUILayout.Button ( "+" ) )
            ChangeVolTableSize ( 1 );
        GUILayout.EndHorizontal ( );
        GUILayout.EndVertical ( );

        GUILayout.EndHorizontal ( );
        GUILayout.EndArea ( );
    }

    string TabSafeTextField(string value) {
        GUI.enabled = !m_HideFields;
        string res = GUILayout.TextField ( value );
        GUI.enabled = true;
        return res;
    }

    void ArraySlider(int[] array, int min, int max) {
        GUILayout.BeginHorizontal ( );

        for ( int i = 0 ; i < array.Length ; i++ ) {
            GUILayout.BeginVertical ( GUILayout.Width(8) );
            array[i] = (int)GUILayout.VerticalSlider ( array [ i ], max, min, GUILayout.Height(64) );
            GUILayout.Box ( array [ i ].ToString ( "X" ), GUILayout.Width(16));
            GUILayout.EndVertical ( );
        }

        GUILayout.EndHorizontal ( );
    }

    void WaveButton(Instruments.InstrumentInstance.Wave wave)
    {
        Instruments.InstrumentInstance ins = instruments.presets[keyboard.currentInstrument];
        bool sel = ins.customWaveform == wave;
        sel = GUILayout.Toggle(sel, wave.ToString());

        if(sel && ins.customWaveform != wave)
        {
            ins.customWaveform = wave;
            instruments.presets[keyboard.currentInstrument] = ins;
        }
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

    private void ChangeVolTableSize(int inc) {
        if ( inc < 0 && instrument.volumeTable.Length <= 1 )
            return;

        Instruments.InstrumentInstance ins = instrument;
        System.Array.Resize ( ref ins.volumeTable, ins.volumeTable.Length + inc );

        if ( inc > 0 )
            ins.volumeTable [ ins.volumeTable.Length - 1 ] = ins.volumeTable [ ins.volumeTable.Length - 2 ];

        instruments.presets [ keyboard.currentInstrument ] = ins;
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
