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
    
    public void SetArray(int[] array) {
        m_Values = array;
        UpdateValues();
    }

    public void SetLoopPoint(int initialValue, UnityAction<float> valueChanged) {
        loopSlider.onValueChanged.RemoveAllListeners();
        loopSlider.value = initialValue;
        loopSlider.onValueChanged.AddListener(valueChanged);
    }

    public void UpdateValues() {
        int arrayCount = m_Values.Length;
        int envCount = m_EnvelopeValues.Count;

        if(envCount < arrayCount) {
            int addCount = arrayCount - envCount;
            for ( int i = 0 ; i < addCount; i++ ) {
                GameObject createdRow = ( GameObject ) Instantiate ( envelopeValuePrefab, transform );
                createdRow.transform.SetAsFirstSibling();
                EnvelopeValue value = createdRow.GetComponent<EnvelopeValue> ( );
                value.GetComponent<Slider>().minValue = minValue;
                value.GetComponent<Slider>().maxValue = maxValue;
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
        loopSlider.maxValue = m_Values.Length;
        length.text = "Length: " + m_Values.Length;
    }

    private void UpdateSliders() {
        for (int i = 0; i < m_EnvelopeValues.Count; i++) {
            int index = m_EnvelopeValues.Count - i - 1;
            m_EnvelopeValues[i].slider.onValueChanged.RemoveAllListeners();
            m_EnvelopeValues[i].slider.value = m_Values[index];
            m_EnvelopeValues[i].AddListener((float value) => {
                OnSliderValueChanged(index, (int)value);
            });
        }
    }

    private void OnSliderValueChanged(int index, int value) {
        m_Values[index] = value;
    }
}
