using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class History : MonoBehaviour {

    public class HistoryEvent
    {
        public HistoryEvent(int patternId, SongData.ColumnEntry[] columns, PatternView.MatrixPosition pos)
        {
            pattern = patternId;
            position = pos;
            this.columns = new SongData.ColumnEntry [ columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                this.columns[ i ] = new SongData.ColumnEntry ( columns [ i ] );
            }
        }

        public PatternView.MatrixPosition position;
        public int pattern;
        public SongData.ColumnEntry[] columns;

        public void Restore(List<SongData.ColumnEntry> data)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                data[columns[i].id] = new SongData.ColumnEntry(columns[i]);
            }
        }
    }

    public SongData data;
    public PatternView view;

    private FiniteStack<HistoryEvent> m_History = new FiniteStack<HistoryEvent>(64);

    public void AddHistroyAtSelection() {
        if ( view.boxSelection.hasSelection ) {
            AddHistoryEntry ( view.boxSelection.selection.startChn, view.boxSelection.selection.chnDelta + 1 );
        } else {
            AddHistoryEntry ( view.position.channel );
        }
    }

    public void AddHistoryEntry(int channel, int count = 1)
    {
        SongData.ColumnEntry[] entries = new SongData.ColumnEntry[count];

        for (int i = 0; i < count; i++)
        {
            entries[i] = data.songData[data.lookupTable[data.currentPattern][channel + i]];
        }

        HistoryEvent evt = new HistoryEvent(data.currentPattern, entries, view.position);
        m_History.Push(evt);
    }

    public void Undo()
    {
        ApplyEvent(m_History);
    }

    private void ApplyEvent(FiniteStack<HistoryEvent> apply)
    {
        if (apply.count <= 0)
            return;

        HistoryEvent next = apply.Pop();
        next.Restore(data.songData);
        data.currentPattern = next.pattern;
        view.UpdatePatternData();
        Debug.Log("Next line is " + next.position.line);
        view.SetSelection(next.position);
    }
}
