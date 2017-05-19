using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EnvelopeValue : MonoBehaviour {
    public Slider slider { get { return m_Slider; } }
    private Slider m_Slider;
    private Text m_Text;

    void Awake() {
        m_Text = GetComponentInChildren<Text> ( );
        m_Slider = GetComponent<Slider> ( );
    }

	void OnValueChange(float value) {
        int val = ( int ) value;
        m_Text.text = val.ToString ( );
    }

    public void AddListener(UnityAction<float> action) {
        m_Slider.onValueChanged.AddListener ( OnValueChange );
        m_Slider.onValueChanged.AddListener(action);
        OnValueChange(m_Slider.value);
    }
}
