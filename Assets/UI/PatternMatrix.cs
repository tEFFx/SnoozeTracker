using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PatternMatrix : MonoBehaviour {
    [HideInInspector]
    public MatrixRow selection;

    public GameObject rowPrefab;
    public SongData data;
    public SongPlayback playback;
    public ScrollRect scrollRect;
    public PatternView patternView;

    private List<MatrixRow> m_PatternRows = new List<MatrixRow>();
    private int m_CurrentPattern = -1;

    void Update() {
        int currCount = m_PatternRows.Count;
        if(currCount != data.numPatterns ) {
            if(currCount < data.numPatterns ) {
                for ( int i = 0 ; i < data.numPatterns - currCount ; i++ ) {
                    GameObject createdRow = ( GameObject ) Instantiate ( rowPrefab, transform );
                    MatrixRow row = createdRow.GetComponent<MatrixRow> ( );
                    row.data = data;
                    row.matrix = this;
                    row.UpdateButtons ( );
                    m_PatternRows.Add ( row );
                }
            } else {
                int remove = currCount - data.numPatterns;
                for ( int i = 0 ; i < remove; i++ ) {
                    DestroyImmediate ( m_PatternRows [ i ].gameObject );
                }
                m_PatternRows.RemoveRange ( 0, remove );
            }

            UpdateMatrix ( );
        }

        if(m_CurrentPattern != data.currentPattern || selection == null ) {
            SetSelectedRow ( m_PatternRows [ data.currentPattern ] );
            m_CurrentPattern = data.currentPattern;
        }
    }

    public void UpdateMatrix() {
        for ( int i = 0 ; i < m_PatternRows.Count ; i++ ) {
            m_PatternRows [ i ].UpdateButtons ( );
        }
    }
	
	public void SetSelectedRow(MatrixRow select) {
        if ( selection != null )
            selection.SetSelected ( false );

        if(select != null ) {
            select.SetSelected ( true );
            selection = select;
        }

        if ( playback.isPlaying && selection != null ) {
            scrollRect.content.localPosition = -selection.transform.localPosition - Vector3.up * 150; //offset 8 rows * 20 - half row (10)
        }

        patternView.UpdatePatternData ( );
    }
}
