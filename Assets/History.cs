using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class History : MonoBehaviour {

    public class HistoryEvent
    {
        public HistoryEvent(params SongData.ColumnEntry[] columns)
        {
            m_Columns = new SongData.ColumnEntry[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                m_Columns[i] = new SongData.ColumnEntry(columns[i]);
            }
        }

        public SongData.ColumnEntry[] m_Columns;

        public void Restore(List<SongData.ColumnEntry> data)
        {
            for (int i = 0; i < m_Columns.Length; i++)
            {
                data[m_Columns[i].id] = new SongData.ColumnEntry(m_Columns[i]);
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

        m_History.Push(new HistoryEvent(entries));
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
    }
}
