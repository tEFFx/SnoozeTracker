using UnityEngine;
using System.Collections;

public class TrackerControls : MonoBehaviour {
    public VirtualKeyboard keyboard;
    public SongData data;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI() {
        Rect rect = new Rect ( Vector2.zero, new Vector2 ( 480, 32 ) );

        GUILayout.BeginArea ( rect );
        GUILayout.BeginHorizontal ( );
        if ( GUILayout.Button ( "-" ) )
            keyboard.currentOctave--;
        GUILayout.Box ( "Octave: " + keyboard.currentOctave );
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
        GUILayout.EndHorizontal ( );
        GUILayout.EndArea ( );
    }

    void ChangePatternSize(int amount) {
        data.SetPatternLength ( data.patternLength + amount );
    }
}
