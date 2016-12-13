﻿using UnityEngine;
using System.Collections;

public class ScopeView : MonoBehaviour {
    public Material lineMat;
    public PSGWrapper psg;
    public Color lineColor;
    public Rect box;
    public int targetFPS;

    private int m_SampleCount = 0;
    private int m_SamplesPerFrame = 0;
    private int m_Channels = 0;

    private float m_LineSpacing;
    private float m_LineHeight;

    private float[] m_SampleData;
    private Vector3 m_ScreenPos;

    void Awake()
    {
        psg.onSampleReadCallback += OnSampleRead;

        m_SamplesPerFrame = AudioSettings.outputSampleRate / targetFPS;

    }

    void OnPostRender()
    {
        GL.PushMatrix();

        lineMat.SetPass(0);
        GL.LoadPixelMatrix();
        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        int end = Mathf.Min(m_SampleData.Length, m_SampleCount + m_SamplesPerFrame - m_Channels);
        m_LineSpacing = box.width / m_SamplesPerFrame;
        m_LineHeight = box.height * 0.5f;

        for (int i = m_SampleCount; i < end; i += m_Channels)
        {
            float s = m_SampleData [ i ];
            if ( m_Channels > 1 )
                s += m_SampleData [ i + 1 ];
            s /= ( float ) m_Channels;

            Vector3 boxPos = new Vector3(box.xMin, Screen.height - (box.yMin + m_LineHeight));
            Vector3 pos = Vector3.zero;
            pos.x += (i % m_SamplesPerFrame) * m_LineSpacing;
            pos.y += s * m_LineHeight;

            GL.Vertex(pos + boxPos);

            s = m_SampleData [ i + m_Channels ];
            if ( m_Channels > 1 )
                s += m_SampleData [ i + m_Channels + 1 ];
            s /= ( float ) m_Channels;

            pos = Vector3.zero;
            pos.x += ((i + m_Channels) % m_SamplesPerFrame) * m_LineSpacing;
            pos.y += s * m_LineHeight;

            GL.Vertex(pos + boxPos);
        }

        
        GL.End();
        GL.PopMatrix();
    }

    void OnGUI()
    {
        GUI.Box(box, "");
    }

    void DrawLine(int index)
    {
        
    }

    void OnSampleRead(float[] data, int channels)
    {
        m_SampleCount = 0;
        m_Channels = channels;
        m_SampleData = data;
    }
}
