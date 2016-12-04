using UnityEngine;
using System.Collections;

public class PatternMatrix : MonoBehaviour {
    public SongData data;
    public Vector2 padding;
    public Vector2 size;
    public Vector2 buttonSize;
    public int selected;
    public Color selectedColor;
    public Color neutralColor;

    private bool m_Inputting;

	void OnGUI()
    {
        Rect rect = new Rect(new Vector2(Screen.width - padding.x - size.x, 0), size);

        GUILayout.BeginArea(rect);

        for (int i = 0; i < data.lookupTable.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUI.color = selected == i ? selectedColor : neutralColor;
            for (int j = 0; j < data.channels; j++)
            {
                if(GUILayout.Button(data.lookupTable[i][j].ToString("X2"), GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                {
                    if(selected == i)
                    {
                        if (Input.GetMouseButtonUp(0))
                            data.IncrementLookup(i, j, 1);
                        else
                            data.IncrementLookup(i, j, -1);
                    }
                    else
                    {
                        selected = i;
                        data.currentPattern = i;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        GUI.color = Color.white;
        if (GUILayout.Button("Add line"))
            data.AddPatternLine();
        GUILayout.EndArea();
    }
}
