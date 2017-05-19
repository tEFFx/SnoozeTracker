using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaveOptions : MonoBehaviour {
	public Dropdown waveSelection;
	public GameObject pwmOptions;
	public SliderValue pwmMin;
	public SliderValue pwmMax;
	public SliderValue pwmSpeed;
	public GameObject sampleOptions;
	public Button loadSample;
	public Button deleteSample;
	public Text sampleInfo;
	public ClickValue waveRelativeNote;
	public Instruments instruments;
	public FileManagement fileManagement;

	private int m_CurrentInstrument;

	void Awake() {
		waveSelection.onValueChanged.AddListener(OnWaveChange);
		waveRelativeNote.onValueChanged = OnNoteChange;
	}

	public void SetData(int insIndex) {
		m_CurrentInstrument = insIndex;
		
		waveSelection.value = !instruments.presets[insIndex].samplePlayback ? 0 : (int) instruments.presets[insIndex].customWaveform + 1;
		waveRelativeNote.value = instruments.presets[insIndex].sampleRelNote;
		
		pwmMin.UpdateValue(instruments.presets[insIndex].pulseWidthMin);
		pwmMax.UpdateValue(instruments.presets[insIndex].pulseWidthMax);
		pwmSpeed.UpdateValue(instruments.presets[insIndex].pulseWidthPanSpeed);
		pwmMin.setValueCallback = x => instruments.presets[insIndex].pulseWidthMin = x;
		pwmMax.setValueCallback = x => instruments.presets[insIndex].pulseWidthMax = x;
		pwmSpeed.setValueCallback = x => instruments.presets[insIndex].pulseWidthPanSpeed = x;
		
		UpdateSampleData();
	}

	public void UpdateSampleData() {
		if (instruments.presets[m_CurrentInstrument].waveTable == null) {
			sampleInfo.text = "NO SAMPLE DATA";
			return;
		}
		int sampleCount = instruments.presets[m_CurrentInstrument].waveTable.Length;
		int sampleRate = instruments.presets[m_CurrentInstrument].waveTableSampleRate;
		sampleInfo.text = sampleCount + " samples / " + sampleRate + " Hz";
	}

	public void LoadSample() {
		if (fileManagement.LoadSample(ref instruments.presets[m_CurrentInstrument].waveTable, ref instruments.presets[m_CurrentInstrument].waveTableSampleRate)) {
			UpdateSampleData();
		}
	}

	public void RemoveSample() {
		instruments.presets[m_CurrentInstrument].waveTable = null;
		UpdateSampleData();
	}

	private void OnWaveChange(int value) {
		if (value == 0) {
			instruments.presets[m_CurrentInstrument].samplePlayback = false;
		}
		else {
			instruments.presets[m_CurrentInstrument].samplePlayback = true;
			instruments.presets[m_CurrentInstrument].customWaveform = (Instruments.InstrumentInstance.Wave) (value - 1);
		}

		pwmOptions.SetActive(value == 1);
		sampleOptions.SetActive(value == 4);
	}

	private void OnNoteChange(int value) {
		instruments.presets[m_CurrentInstrument].sampleRelNote = value;
	}
}
