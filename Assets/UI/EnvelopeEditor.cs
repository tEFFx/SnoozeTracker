using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EnvelopeEditor : MonoBehaviour {
    public int minValue;
    public int maxValue;
    public GameObject envelopeValuePrefab;
    public Slider loopSlider;
    public Text length;
    public Button increaseArray;
    public Button decreaseArray;

    private int[] m_Values;
    private List<EnvelopeValue> m_EnvelopeValues = new List<EnvelopeValue>();
    private Action<int> m_LoopChanged;
    private bool m_UpdateLoopSlider;

    void Awake() {
        loopSlider.onValueChanged.AddListener ( OnLoopChanged );
    }
    
    public void SetArray(int[] array) {
        m_Values = array;
        UpdateValues();
    }

    public void SetLoopPoint(int initialValue, Action<int> valueChanged) {
        m_UpdateLoopSlider = false;
        loopSlider.value = initialValue;
        m_LoopChanged = valueChanged;
        m_UpdateLoopSlider = true;
    }

    public void UpdateValues() {
        if ( m_Values == null )
            return;

        int arrayCount = m_Values.Length;
        int envCount = m_EnvelopeValues.Count;

        if(envCount < arrayCount) {
            int addCount = arrayCount - envCount;
            for ( int i = 0 ; i < addCount; i++ ) {
                GameObject createdRow = ( GameObject ) Instantiate ( envelopeValuePrefab, transform );
                createdRow.transform.SetAsFirstSibling();
                EnvelopeValue value = createdRow.GetComponent<EnvelopeValue> ( );
                m_EnvelopeValues.Add ( value );
            }
        } else {
            int remove = envCount - arrayCount;
            for ( int i = 0 ; i < remove; i++ ) {
                m_EnvelopeValues[i].slider.onValueChanged.RemoveAllListeners();
                DestroyImmediate ( m_EnvelopeValues [ i ].gameObject );
            }
            m_EnvelopeValues.RemoveRange ( 0, remove );
        }
        
        UpdateSliders();
        m_UpdateLoopSlider = false;
        loopSlider.maxValue = m_Values.Length;
        m_UpdateLoopSlider = true;
        length.text = "Length: " + m_Values.Length;
    }

    private void UpdateSliders() {
        for (int i = 0; i < m_EnvelopeValues.Count; i++) {
            int index = m_EnvelopeValues.Count - i - 1;
            m_EnvelopeValues[i].slider.onValueChanged.RemoveAllListeners();
            m_EnvelopeValues[i].slider.value = m_Values[index];
            m_EnvelopeValues [ i ].GetComponent<Slider> ( ).minValue = minValue;
            m_EnvelopeValues [ i ].GetComponent<Slider> ( ).maxValue = maxValue;
            m_EnvelopeValues [i].AddListener((float value) => {
                OnSliderValueChanged(index, (int)value);
            });
        }
    }

    private void OnSliderValueChanged(int index, int value) {
        m_Values[index] = value;
    }

    private void OnLoopChanged(float value) {
        if ( m_UpdateLoopSlider )
            m_LoopChanged ( ( int ) value );
    }
}
