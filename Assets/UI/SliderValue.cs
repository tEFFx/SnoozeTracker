using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SliderValue : MonoBehaviour {
	public Action<int> setValueCallback;
	public Text textValue;
	public Slider slider;
	private bool m_UpdateValue = true;
	
	void Awake() {
		slider = GetComponentInChildren<Slider>();
		slider.onValueChanged.AddListener(OnValueChange);
	}

	void OnValueChange(float value) {
		int val = (int) value;
		
		if (m_UpdateValue && setValueCallback != null)
			setValueCallback(val);

		textValue.text = val.ToString("X2");
	}

	public void UpdateValue(int value) {
		m_UpdateValue = false;
		slider.value = value;
		m_UpdateValue = true;
	}
}
