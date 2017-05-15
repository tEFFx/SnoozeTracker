using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TrackerControls : MonoBehaviour {
    public ClickValue octave;
    public ClickValue loops;
    public ClickValue patternLen;
    public InputField artistName;
    public InputField songName;

    public VirtualKeyboard keyboard;
    public SongData data;
    public FileManagement fileMan;
    public SongPlayback playback;

    private bool m_HideTextFields;

	// Use this for initialization
	void Start () {
        octave.value = keyboard.currentOctave;
        patternLen.value = data.patternLength;

        octave.onValueChanged += (int val) => {
            keyboard.currentOctave = val;
        };
        loops.onValueChanged += (int val) => {
            playback.loops = val;

            if ( val == -1 )
                loops.textValue.text = "Loops: ∞";
        };
        loops.value = playback.loops;
        patternLen.onValueChanged += (int val) => {
            if ( !playback.isPlaying )
                data.SetPatternLength ( val );
            else
                patternLen.SetValue ( data.patternLength );
        };

        songName.onValueChanged.AddListener ( (string val) => {
            SongData.songName = val;
        } );
        artistName.onValueChanged.AddListener ( (string val) => {
            SongData.artistName = val;
         } );

        fileMan.onFileOpen += () => {
            songName.text = SongData.songName;
            artistName.text = SongData.artistName;
            patternLen.value = data.patternLength;
        };
	}
}
