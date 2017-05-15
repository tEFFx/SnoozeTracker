using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct BoxSelectionRange {
    public int startLine;
    public int endLine;
    public int startCol;
    public int endCol;
    public int startChn;
    public int endChn;

    public int lineDelta { get { return endLine - startLine; } }
    public int chnDelta { get { return endChn - startChn; } }
}

public class BoxSelection : MonoBehaviour {
    public delegate void SelectionDataUpdateDelegate(int line, int chn, int col);
    public bool hasSelection { get { return m_HasSelection; } }
    public BoxSelectionRange selection { get { return m_Selection; } }

    public PatternView view;
    public Image selectionBox;
    private bool m_Selecting;

    private BoxSelectable m_InitialSelection;
    private BoxSelectable m_LastSelection;

    private bool m_HasSelection;
    private BoxSelectionRange m_Selection;

    public void StartSelection(BoxSelectable start) {
        if (m_Selecting)
            return;

        m_Selecting = true;
        selectionBox.gameObject.SetActive(true);
        selectionBox.rectTransform.anchorMax = selectionBox.rectTransform.anchorMin = Vector3.zero;
        m_InitialSelection = m_LastSelection = start;
        UpdateSelectionBox();
    }

    public void UpdateSelection(BoxSelectable next) {
        if (!m_Selecting)
            return;

        m_LastSelection = next;
        UpdateSelectionBox();
    }

    public void SetSelection(BoxSelectable first, BoxSelectable last) {
        if(first == last) {
            selectionBox.gameObject.SetActive ( false );
            m_HasSelection = false;
            return;
        }

        if(!selectionBox.gameObject.activeSelf)
            selectionBox.gameObject.SetActive ( true );

        m_InitialSelection = first;
        m_LastSelection = last;
        UpdateSelectionBox ( );
        m_HasSelection = true;
        UpdateSelectionData ( );
    }

    public void DoOperation(SelectionDataUpdateDelegate op, bool update = true) {
        for (int line = m_Selection.startLine; line <= m_Selection.endLine; line++) {
            for (int chn = m_Selection.startChn; chn <= m_Selection.endChn; chn++) {
                int startCol = chn == m_Selection.startChn ? m_Selection.startCol : 0;
                int endCol = chn == m_Selection.endChn ? m_Selection.endCol : SongData.SONG_DATA_COUNT - 1;
                for (int col = startCol; col <= endCol; col++) {
                    op(line, chn, col);
                }

                if(update)
                    view.UpdateSingleRow(chn, line);
            }
        }
    }

    void Update() {
        if(m_Selecting && Input.GetMouseButtonUp(0)) {
            m_Selecting = false;

            if(m_InitialSelection == m_LastSelection) {
                selectionBox.gameObject.SetActive(false);
                m_HasSelection = false;
            } else {
                m_HasSelection = true;
            }

            UpdateSelectionData ( );
        }
    }

    void UpdateSelectionData() {
        m_Selection = new BoxSelectionRange ( );
        m_Selection.startLine = Mathf.Min ( m_InitialSelection.row.line, m_LastSelection.row.line );
        m_Selection.endLine = Mathf.Max ( m_InitialSelection.row.line, m_LastSelection.row.line );
        m_Selection.startChn = Mathf.Min ( m_InitialSelection.row.channel, m_LastSelection.row.channel );
        m_Selection.endChn = Mathf.Max ( m_InitialSelection.row.channel, m_LastSelection.row.channel );
        m_Selection.startCol = Mathf.Min ( m_InitialSelection.col, m_LastSelection.col );
        m_Selection.endCol = Mathf.Max ( m_InitialSelection.col, m_LastSelection.col );
    }

    void UpdateSelectionBox() {
        Vector3 min = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 max = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);
        Vector3[] corners = new Vector3[4];

        m_InitialSelection.rectTransform.GetWorldCorners(corners);
        for (int i = 0; i < 4; i++) {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }

        m_LastSelection.rectTransform.GetWorldCorners(corners);
        for (int i = 0; i < 4; i++) {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }

        min = transform.InverseTransformPoint(min);
        max = transform.InverseTransformPoint(max);

        selectionBox.rectTransform.sizeDelta = max - min;
        selectionBox.rectTransform.localPosition = (max + min) * 0.5f;
    }

}
