using UnityEngine;
using System.Collections;

public class TrackerControls : MonoBehaviour {
    public VirtualKeyboard keyboard;
    public SongData data;
    public FileManagement fileMan;

    private bool m_HideTextFields;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI() {
        if ( Event.current.keyCode == KeyCode.Tab ) {
            if ( Event.current.type == EventType.KeyDown )
                m_HideTextFields = true;
            else
                m_HideTextFields = false;
        }

        Rect rect = new Rect ( Vector2.zero, new Vector2 ( Screen.width, 32 ) );

        GUILayout.BeginArea ( rect );
        GUILayout.BeginHorizontal ( );
        if ( GUILayout.Button ( "-" ) )
            keyboard.currentOctave--;
        GUILayout.Box ( "Octave: " + keyboard.currentOctave);
        if ( GUILayout.Button ( "+" ) )
            keyboard.currentOctave++;

        GUILayout.Space ( 16 );
        if ( GUILayout.Button ( "--" ) )
            ChangePatternSize ( -8 );
        if ( GUILayout.Button ( "-" ) )
            ChangePatternSize ( -1 );
        GUILayout.Box ( "Pattern length: " + data.patternLength );
        if ( GUILayout.Button ( "+" ) )
            ChangePatternSize ( 1 );
        if ( GUILayout.Button ( "++" ) )
            ChangePatternSize ( 8 );

        GUILayout.Space ( 16 );
        if ( GUILayout.Button ( "Save" ) )
            fileMan.SaveFile ( );
        if ( GUILayout.Button ( "Open" ) )
            fileMan.OpenFile ( );
        if (GUILayout.Button("VGM"))
            fileMan.SaveVGM();
        if (GUILayout.Button("WAVE"))
            fileMan.SaveWAV();

        GUI.enabled = !m_HideTextFields;
        GUILayout.Box ( "Artist: " );
        SongData.artistName = GUILayout.TextField ( SongData.artistName, GUILayout.Width ( 160 ) );
        GUILayout.Box ( "Song name: " );
        SongData.songName = GUILayout.TextField ( SongData.songName, GUILayout.Width ( 160 ) );
        GUI.enabled = true;

        GUILayout.FlexibleSpace ( );
        GUILayout.EndHorizontal ( );
        GUILayout.EndArea ( );
    }

    void ChangePatternSize(int amount) {
        data.SetPatternLength ( data.patternLength + amount );
    }
}
