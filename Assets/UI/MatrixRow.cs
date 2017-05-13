using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatrixRow : MonoBehaviour {
    public int row { get { return transform.GetSiblingIndex ( ); } }

    public PatternMatrix matrix;
    public SongData data;
    public Color selectionColor;
    public Color neutralColor;

    private MatrixButton[] m_Buttons;
    private bool m_Selected;

	void Awake () {
        m_Buttons = GetComponentsInChildren<MatrixButton> ( );

        for ( int i = 0 ; i < m_Buttons.Length ; i++ ) {
            int buttonIndex = i;
            m_Buttons [ i ].onClick = (int mouseButton) => { Debug.Log ( "HELLO" ); ButtonClick ( mouseButton, buttonIndex ); };
        }
	}

    void Update() {
        if ( Input.GetKeyDown ( KeyCode.LeftControl ) || Input.GetKeyUp ( KeyCode.LeftControl ) )
            UpdateButtons ( );            
    }

    public void SetSelected(bool selected) {
        for ( int i = 0 ; i < m_Buttons.Length ; i++ ) {
            m_Buttons [ i ].image.color = selected ? selectionColor : neutralColor;
        }

        m_Selected = selected;
    }

    private void ButtonClick(int button, int index) {
        Debug.Log ( "Click on row " + row + ", col " + index );
        if( m_Selected ) {
            if ( Input.GetKey ( KeyCode.LeftControl ) ) {
                if ( button == 0 )
                    data.transposeTable [ row ] [ index ]++;
                else if(button == 1)
                    data.transposeTable [ row ] [ index ]--;
            } else {
                int inc = 1;

                if ( Input.GetKey ( KeyCode.LeftShift ) )
                    inc = 16;

                if ( button == 0 )
                    data.IncrementLookup ( row, index, inc );
                else if(button == 1)
                    data.IncrementLookup ( row, index, -inc );
            }

            UpdateButton ( index );
            data.patternView.UpdatePatternChannel(index);
        } else {
            data.currentPattern = row;
        }
    }

    public void UpdateButton(int index) {
        bool ctrlDown = Input.GetKey ( KeyCode.LeftControl );
        int tableVal = ctrlDown ? data.transposeTable [ row ] [ index ] : data.lookupTable [ row ] [ index ];
        string label = tableVal >= 0 ? tableVal.ToString ( "X2" ) : "X";
        if ( ctrlDown )
            label = tableVal.ToString ( );
        m_Buttons [ index ].GetComponentInChildren<Text> ( ).text = label;
    }

    public void UpdateButtons() {
        for ( int i = 0 ; i < m_Buttons.Length ; i++ ) {
            UpdateButton ( i );
        }
    }
}
