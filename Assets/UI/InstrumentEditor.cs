using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstrumentEditor : MonoBehaviour {
    public GameObject instrumentPrefab;

    public PatternView view;
    public Instruments instruments;
    public VirtualKeyboard keyboard;
    public Button initialTab;
    public EnvelopeEditor volumeEnvelope;
    public GameObject volumeParent;
    public EnvelopeEditor arpeggioEnvelope;
    public GameObject arpeggioParent;
    public EnvelopeEditor noiseEnvelope;
    public GameObject noiseParent;
    public WaveOptions waveOptions;
    public Toggle absoluteNotes;
    [Tooltip("xy=relative minmax, zw=absolute minmax")]
    public Vector4 noteRange;

    private List<InstrumentButton> m_Instruments = new List<InstrumentButton>();
    private InstrumentButton m_SelectedInstrument;
    private int m_EditorState;
    private Button m_SelectedEnv;

    void Start() {
        volumeEnvelope.increaseArray.onClick.AddListener(() => {
            instruments.presets[keyboard.currentInstrument].ResizeVolumeTable(1);
            UpdateEnvelopes();
        });
        volumeEnvelope.decreaseArray.onClick.AddListener(() => {
            instruments.presets[keyboard.currentInstrument].ResizeVolumeTable(-1);
            UpdateEnvelopes();
        });
        
        arpeggioEnvelope.decreaseArray.onClick.AddListener(() => {
            instruments.presets[keyboard.currentInstrument].ResizeArpTable(-1);
            UpdateEnvelopes();
        });
        arpeggioEnvelope.increaseArray.onClick.AddListener(() => {
            instruments.presets[keyboard.currentInstrument].ResizeArpTable(1);
            UpdateEnvelopes();
        });

        noiseEnvelope.decreaseArray.onClick.AddListener ( () => {
            instruments.presets [ keyboard.currentInstrument ].ResizeNoiseTable ( -1 );
            UpdateEnvelopes ( );
        } );
        noiseEnvelope.increaseArray.onClick.AddListener ( () => {
            instruments.presets [ keyboard.currentInstrument ].ResizeNoiseTable ( 1 );
            UpdateEnvelopes ( );
        } );
        
        HighlightTab(initialTab);
    }
    
	// Update is called once per frame
	void Update () {
        UpdateInstruments ( );
    }

    public void UpdateInstruments() {
        int currCount = m_Instruments.Count;
        int presets = instruments.presets.Length;

        if ( currCount == presets )
            return;

        if(currCount < presets) {
            int add = presets - currCount;
            for ( int i = 0 ; i < add ; i++ ) {
                GameObject obj = Instantiate ( instrumentPrefab, transform );
                InstrumentButton btnInstance = obj.GetComponent<InstrumentButton> ( );
                btnInstance.editor = this;
                m_Instruments.Add ( btnInstance );
            }
        } else {
            int remove = currCount - presets;
            for ( int i = 0 ; i < remove ; i++ ) {
                DestroyImmediate ( m_Instruments [ i ].gameObject );
            }

            m_Instruments.RemoveRange ( 0, remove );
        }

        UpdateInstrumentInfo ( );

        if ( m_SelectedInstrument == null )
            SetSelectedInstrument ( 0 );
        
        SetEditorState(m_EditorState);
    }

    public void UpdateInstrumentInfo() {
        for ( int i = 0 ; i < m_Instruments.Count ; i++ ) {
            m_Instruments [ i ].UpdateInfo ( );
        }
    }

    public void SetSelectedInstrument(int index) {
        if (index >= instruments.presets.Length)
            return;
        
        if ( m_SelectedInstrument != null )
            m_SelectedInstrument.SetSelected ( false );

        m_SelectedInstrument = m_Instruments [ index ];
        m_SelectedInstrument.SetSelected ( true );

        keyboard.currentInstrument = index;

        absoluteNotes.onValueChanged.RemoveAllListeners ( );
        absoluteNotes.isOn = instruments.presets [ index ].arpAbsolute;
        UpdateAbsNotes ( false );
        absoluteNotes.onValueChanged.AddListener ( (bool val) => { instruments.presets [ index ].arpAbsolute = val; UpdateAbsNotes ( ); } );

        UpdateEnvelopes ( );

        volumeEnvelope.SetLoopPoint(instruments.presets[index].volumeLoopPoint, x => instruments.presets[index].volumeLoopPoint = (int)x );
        arpeggioEnvelope.SetLoopPoint(instruments.presets[index].arpLoopPoint, x => instruments.presets[index].arpLoopPoint = (int)x);
        noiseEnvelope.SetLoopPoint ( instruments.presets [ index ].noiseModeLoopPoint, x => instruments.presets [ index ].noiseModeLoopPoint = ( int ) x );
        waveOptions.SetData(index);
    }

    public void SetEditorState(int state) {
        volumeParent.gameObject.SetActive(state == 0);
        arpeggioParent.gameObject.SetActive(state == 1);
        noiseParent.gameObject.SetActive ( state == 2 );
        waveOptions.gameObject.SetActive(state == 3);
        m_EditorState = state;
    }

    public void NewInstrument() {
        instruments.CreateInstrument ( );
        UpdateEnvelopes();
        view.UpdatePatternData();
        UpdateInstruments();
        SetSelectedInstrument(instruments.presets.Length - 1);
    }

    public void RemoveInstrument() {
        if (instruments.presets.Length <= 1)
            return;

        int ins = keyboard.currentInstrument;
        
        instruments.RemoveInstrument ( ins );

        List<SongData.ColumnEntry> data = view.data.songData;
        for (int e = 0; e < data.Count; e++) {
            int[,] val = data[e].data;
            for (int r = 0; r < val.GetLength(0); r++) {
                if (val[r, 1] == ins)
                    val[r, 1] = instruments.presets.Length;
                else if(val[r, 1] > ins && val[r, 1] <= instruments.presets.Length)
                    val[r, 1]--;
            }
        }

        UpdateInstruments();

        if (ins < instruments.presets.Length)
            SetSelectedInstrument(ins); 
        else if (ins >= instruments.presets.Length)
            SetSelectedInstrument(ins - 1);
        
        view.UpdatePatternData();
    }

    public void CopyInstrument() {
        instruments.CopyInstrument(keyboard.currentInstrument);
        UpdateEnvelopes();
        view.UpdatePatternData();
        UpdateInstruments();
        SetSelectedInstrument(instruments.presets.Length - 1);
    }

    public void HighlightTab(Button selection) {
        if (m_SelectedEnv != null)
            m_SelectedEnv.image.color = Color.white;

        selection.image.color = Color.red;
        m_SelectedEnv = selection;
    }

    private void UpdateEnvelopes() {
        volumeEnvelope.SetArray(instruments.presets[keyboard.currentInstrument].volumeTable);
        arpeggioEnvelope.SetArray(instruments.presets[keyboard.currentInstrument].arpeggio);
        noiseEnvelope.SetArray ( instruments.presets [ keyboard.currentInstrument ].noiseMode );
        waveOptions.SetData(keyboard.currentInstrument);
    }

    private void UpdateAbsNotes(bool updateValue = true) {
        arpeggioEnvelope.minValue = instruments.presets [ keyboard.currentInstrument ].arpAbsolute ? ( int ) noteRange.z : ( int ) noteRange.x;
        arpeggioEnvelope.maxValue = instruments.presets [ keyboard.currentInstrument ].arpAbsolute ? ( int ) noteRange.w : ( int ) noteRange.y;

        if(updateValue)
            arpeggioEnvelope.UpdateSliders ( );
    }
}
