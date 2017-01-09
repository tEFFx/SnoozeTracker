using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class History : MonoBehaviour {

    public class HistoryEvent
    {
        public HistoryEvent(int patternId, params SongData.ColumnEntry[] columns)
        {
            pattern = patternId;

            this.columns = new SongData.ColumnEntry [ columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                this.columns[ i ] = new SongData.ColumnEntry ( columns [ i ] );
            }
        }

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
        if ( view.multipleSelection ) {
            AddHistoryEntry ( view.GetChannelSelection ( view.dragSelectStart ), view.GetChannelSelection ( view.dragSelectStart + view.dragSelectOffset ) );
        } else {
            AddHistoryEntry ( view.selectedChannel );
        }
    }

    public void AddHistoryEntry(params int[] columns)
    {
        SongData.ColumnEntry[] entries = new SongData.ColumnEntry[columns.Length];

        for (int i = 0; i < columns.Length; i++)
        {
            entries[i] = data.songData[data.lookupTable[data.currentPattern][columns[i]]];
        }

        m_History.Push(new HistoryEvent(data.currentPattern, entries));
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
    }
}
