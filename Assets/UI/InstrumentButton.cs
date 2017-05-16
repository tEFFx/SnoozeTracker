using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstrumentButton : MonoBehaviour {
    public Text instrumentIndex;
    public Text buttonText;
    public Button button;
    public InputField input;
    public Color selection;
    public InstrumentEditor editor;

    private bool m_Selected;
    private bool m_EditingName;
    private int m_InstrumentId;

    void Start() {
        button.onClick.AddListener ( OnButtonClick );
    }

    void Update() {
        if ( m_EditingName ) {
            if ( Input.GetKey ( KeyCode.Return ) || Input.GetMouseButton ( 0 ) ) {
                SetEditing ( false );

                string newName = input.text;
                buttonText.text = newName;
                //store name!!!
            }
        }
    }

    void OnButtonClick() {
        if ( !m_Selected ) {
            editor.SetSelectedInstrument ( m_InstrumentId );
        } else {
            SetEditing ( true );
        }
    }

	public void UpdateInfo() {
        m_InstrumentId = transform.GetSiblingIndex ( );

        Instruments.InstrumentInstance ins = editor.instruments.presets [ m_InstrumentId ];

        instrumentIndex.text = m_InstrumentId.ToString ( "X2" );

        string name = ins.name == string.Empty ? "Instrument " + m_InstrumentId : ins.name;
        buttonText.text = name;
        input.text = name;
    }

    public void SetSelected(bool selected) {
        m_Selected = selected;

        button.image.color = input.image.color = selected ? selection : Color.white;
    }

    private void SetEditing(bool active) {
        if ( m_EditingName == active )
            return;

        button.gameObject.SetActive ( !active );
        input.gameObject.SetActive ( active );
        editor.keyboard.enabled = !active;
        DeselectOnClick.enabled = !active;

        if ( active )
            input.Select ( );

        m_EditingName = active;
    }
}
