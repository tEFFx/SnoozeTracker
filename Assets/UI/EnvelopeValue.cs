using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EnvelopeValue : MonoBehaviour {
    public Slider slider { get { return GetComponent<Slider>(); } }
    public Text text {get { return GetComponentInChildren<Text>(); }}
    public string format = "D";
    
	void OnValueChange(float value) {
        int val = ( int ) value;
	    text.text = val.ToString ( );
    }

    public void AddListener(UnityAction<float> action) {
        slider.onValueChanged.AddListener ( OnValueChange );
        slider.onValueChanged.AddListener(action);
        OnValueChange(slider.value);
    }
}
