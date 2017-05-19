using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ScopeView : MonoBehaviour {
    public Material lineMat;
    public PSGWrapper psg;
    public int targetFPS;
    public Color clearColor;

    private int m_SampleCount = 0;
    private int m_SamplesPerFrame = 0;
    private int m_Channels = 0;

    private float m_LineSpacing;
    private float m_LineHeight;

    private float[] m_SampleData;
    private Vector3 m_ScreenPos;
    private RenderTexture m_Target;
    private Vector2 m_Size;

    void Awake()
    {
        psg.onSampleReadCallback += OnSampleRead;

        m_SamplesPerFrame = AudioSettings.outputSampleRate / targetFPS;

    }

    void Start() {
        Image img = GetComponent<Image>();
        int w = (int) img.rectTransform.rect.size.x;
        int h = (int) img.rectTransform.rect.size.y;
        m_Target = new RenderTexture(w, h, 0);
        m_Size = img.rectTransform.rect.size;
        img.material.mainTexture = m_Target;
    }

    void Update()
    {
        if ( m_SampleData == null )
            return;

        lineMat.SetPass(0);
        RenderTexture.active = m_Target;
        
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, m_Target.width, m_Target.height, 0);
        GL.Clear(false, true, clearColor);
        GL.Begin(GL.LINES);

        int end = Mathf.Min(m_SampleData.Length, m_SampleCount + m_SamplesPerFrame - m_Channels);
        m_LineSpacing = m_Size.x / m_SamplesPerFrame;
        m_LineHeight = m_Size.y * 0.5f;

        for (int i = m_SampleCount; i < end; i += m_Channels)
        {
            float s = m_SampleData [ i ];
            if ( m_Channels > 1 )
                s += m_SampleData [ i + 1 ];
            s /= ( float ) m_Channels;
            s = Mathf.Min ( s, 0.9f );

            Vector3 pos = Vector3.zero;
            pos.x += (i % m_SamplesPerFrame) * m_LineSpacing;
            pos.y += s * m_LineHeight + m_LineHeight;

            GL.Vertex(pos);

            s = m_SampleData [ i + m_Channels ];
            if ( m_Channels > 1 )
                s += m_SampleData [ i + m_Channels + 1 ];
            s /= ( float ) m_Channels;

            pos = Vector3.zero;
            pos.x += ((i + m_Channels) % m_SamplesPerFrame) * m_LineSpacing;
            pos.y += s * m_LineHeight + m_LineHeight;

            GL.Vertex(pos);
        }

        
        GL.End();
        GL.PopMatrix();
        RenderTexture.active = null;
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
