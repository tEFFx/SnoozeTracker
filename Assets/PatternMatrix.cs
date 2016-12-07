using UnityEngine;
using System.Collections;

public class PatternMatrix : MonoBehaviour {
    public SongData data;
    public PatternView view;
    public Vector2 padding;
    public Vector2 size;
    public Vector2 buttonSize;
    public Color selectedColor;
    public Color neutralColor;

    private bool m_Inputting;
    private Vector2 m_Scroll;

	void OnGUI()
    {
        Rect rect = new Rect(new Vector2(padding.x, padding.y), size);

        GUILayout.BeginArea(rect);
        m_Scroll = GUILayout.BeginScrollView ( m_Scroll, false, true );
        for (int i = 0; i < data.lookupTable.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUI.color = Color.blue;
            GUI.color = data.currentPattern == i ? selectedColor : neutralColor;
            for (int j = 0; j < data.channels; j++)
            {
                if(GUILayout.Button(data.lookupTable[i][j].ToString("X2"), GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                {
                    if( data.currentPattern == i)
                    {
                        if (Input.GetMouseButtonUp(0))
                            data.IncrementLookup(i, j, 1);
                        else
                            data.IncrementLookup(i, j, -1);
                    }
                    else
                    {
                        data.currentPattern = i;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView ( );
        GUI.color = Color.white;
        GUILayout.BeginHorizontal ( );
        if (GUILayout.Button("Insert"))
            data.AddPatternLine();
        if ( GUILayout.Button ( "Delete" ) )
            data.DeletePatternLine ( );        
        if ( GUILayout.Button ( "Copy" ) ) 
            data.CopyPatternLine ( );
        
        GUILayout.EndHorizontal ( );
        GUILayout.EndArea();
    }
}
