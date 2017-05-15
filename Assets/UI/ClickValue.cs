using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ClickValue : MonoBehaviour {
    public int value {
        get { return m_Value; }
        set {
            if ( value < min || value > max )
                return;

            SetValue ( value );

            if ( onValueChanged != null )
                onValueChanged ( value );
        }
    }

    public Text textValue;
    public int bigStep = 10;
    public int smallStep = 1;
    public int min;
    public int max;
    public string prefix;
    public Action<int> onValueChanged;

    private int m_Value;

	// Use this for initialization
	void Awake () {
        Button[] buttons = GetComponentsInChildren<Button> ( );
        if ( bigStep == smallStep ) {
            Destroy ( buttons [ 0 ].gameObject );
            Destroy ( buttons [ 3 ].gameObject );
        } else {
            buttons [ 0 ].onClick.AddListener ( () => { value -= bigStep; } );
            buttons [ 3 ].onClick.AddListener ( () => { value += bigStep; } );
        }

        buttons [ 1 ].onClick.AddListener ( () => { value -= smallStep; } );
        buttons [ 2 ].onClick.AddListener ( () => { value += smallStep; } );
    }

    public void SetValue(int value) {
        m_Value = value;
        textValue.text = prefix + value.ToString ( );
    }
}
