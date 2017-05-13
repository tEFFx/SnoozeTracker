using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoiceButtons : MonoBehaviour {
    public Button[] buttons;
    public RectTransform[] bars;
    public SongPlayback playback;
    public float doubleClickTimeout;

    private float[] m_LastClicks;
    private bool m_WasPlaying = true;

	// Use this for initialization
	void Start () {
        m_LastClicks = new float[buttons.Length];
        for (int i = 0; i < buttons.Length; i++) {
            int chn = i;
            buttons[i].onClick.AddListener(() => { ToggleMute(chn); });
            SetButtonText(i, "PSG" + i);
        }
	}

    void Update() {
        if (playback.isPlaying) {
            for (int i = 0; i < bars.Length; i++) {
                bars[i].transform.localScale = new Vector3(playback.chnAttn[i] / 16f, 1, 1);
            }

            m_WasPlaying = true;
        }else if (m_WasPlaying) {
            m_WasPlaying = false;

            for (int i = 0; i < bars.Length; i++) {
                bars[i].transform.localScale = Vector3.zero;
            }
        }
    }

	public void ToggleMute(int channel) {
        if (Time.time - m_LastClicks[channel] < doubleClickTimeout) {
            bool mute = playback.mute[channel];
            for (int i = 0; i < buttons.Length; i++) {
                if (i == channel)
                    continue;

                SetMute(i, mute);
            }
        } else {
            m_LastClicks[channel] = Time.time;
        }

        SetMute(channel, !playback.mute[channel]);
    }

    private void SetMute(int channel, bool mute) {
        string text = "PSG" + channel;
        if (mute)
            text += " (muted)";
        SetButtonText(channel, text);
        playback.mute[channel] = mute;
    }

    private void SetButtonText(int button, string text) {
        buttons[button].GetComponentInChildren<Text>().text = text;
    }
}
