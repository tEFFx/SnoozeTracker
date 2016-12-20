using UnityEngine;
using System.Collections;

public class FiniteStack<T> {
    public FiniteStack(int size){
        m_Items = new T[size];
    }

    public int count { get { return m_Count; } }

    private T[] m_Items;
    private int m_Top = 0;
    private int m_Count = 0;

    public void Push(T item)
    {
        m_Items[m_Top] = item;
        m_Top = (m_Top + 1) % m_Items.Length;
        m_Count++;
    }

    public T Pop()
    {
        m_Top = (m_Items.Length + m_Top - 1) % m_Items.Length;
        m_Count--;
        return m_Items[m_Top];
    }
}
