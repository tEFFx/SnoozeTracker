using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushoutQueue<T> {
    public PushoutQueue(int size) {
        m_Items = new T[size];
    }

    private T[] m_Items;
    private int m_Top = 0;
    private int m_Back = 0;
    private int m_Count = 0;

    public void Enqueue(T item) {
        m_Items[m_Top] = item;
        m_Top = (m_Top + 1) % m_Items.Length;
        m_Count++; 
    }

    public T Dequeue() {
        T item = m_Items[m_Back];
        m_Back = (m_Back + 1) % m_Items.Length;
        m_Count--;
        return item;
    }
}
