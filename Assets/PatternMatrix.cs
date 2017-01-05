using UnityEngine;
using System.Collections;

public class PatternMatrix : MonoBehaviour {
    public SongData data;
    public PatternView view;
    public SongPlayback playback;
    public Vector2 padding;
    public Vector2 size;
    public Vector2 buttonSize;
    public Color selectedColor;
    public Color neutralColor;

    private bool m_Inputting;
    private Vector2 m_Scroll;

    void Update() {
        if ( playback.isPlaying ) {
            m_Scroll.y = Mathf.Max(0, data.currentPattern * (buttonSize.y + 8) - size.y);
        }
    }

	void OnGUI()
    {
        Rect rect = new Rect(new Vector2(padding.x, padding.y), size);

        GUILayout.BeginArea(rect);
        GUILayout.BeginHorizontal ( );
        GUILayout.BeginVertical ( GUILayout.Width(24) );
        if ( GUILayout.Button ( "▲" ) )
            data.MovePattern ( -1 );
        if ( GUILayout.Button ( "▼" ) )
            data.MovePattern ( 1 );
        if ( GUILayout.Button ( "+" ) )
            data.AddPatternLine ( );
        if ( GUILayout.Button ( "-" ) )
            data.DeletePatternLine ( );
        if ( GUILayout.Button ( "=" ) )
            data.CopyPatternLine ( );
        GUILayout.EndVertical ( );

        Vector2 scroll = GUILayout.BeginScrollView ( m_Scroll, false, true );
        if ( !playback.isPlaying )
            m_Scroll = scroll;

        for (int i = 0; i < data.lookupTable.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUI.color = Color.blue;
            GUI.color = data.currentPattern == i ? selectedColor : neutralColor;
            for (int j = 0; j < data.channels; j++)
            {
                bool ctrlDown = Input.GetKey(KeyCode.LeftControl);
                int tableVal = ctrlDown ? data.transposeTable [ i ] [ j ] : data.lookupTable [ i ] [ j ];
                string label = tableVal >= 0 ? tableVal.ToString ( "X2" ) : "X";
                if (ctrlDown)
                    label = tableVal.ToString();
                if (GUILayout.Button(label, GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)) && !playback.isPlaying)
                {
                    if( data.currentPattern == i)
                    {
                        if (ctrlDown) {
                            if ( Input.GetMouseButtonUp ( 0 ) )
                                data.transposeTable [ i ] [ j ]++;
                            else
                                data.transposeTable [ i ] [ j ]--;
                        } else {
                            int inc = Input.GetKey ( KeyCode.LeftShift ) ? 16 : 1;

                            if ( Input.GetMouseButtonUp ( 0 ) )
                                data.IncrementLookup ( i, j, inc );
                            else
                                data.IncrementLookup ( i, j, -inc );
                        }
                    }
                    else
                    {
                        data.currentPattern = i;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndScrollView ( );
        GUILayout.EndArea();
    }
}
