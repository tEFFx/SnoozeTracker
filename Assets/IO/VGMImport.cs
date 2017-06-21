using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class VGMImport : MonoBehaviour
{
	public SongData data;
	public PatternView view;
	public PatternMatrix matrix;
	public Instruments instruments;

	private int m_CurrRow;
	public void ImportVGMFile(BinaryReader reader)
	{
		/* 	TODO:
			Make sure that instrument editor is updated
			Make sure tracker controls are updated (specifically pattern length)
		 */
		
		instruments.CreateInstrument();
		instruments.presets[0].volumeTable = new int[] {0xF};
		instruments.presets[1].volumeTable = new int[] {0xF};

		data.SetPatternLength(128);
		data.SetData(0, 0, 3, 0xf);
		data.SetData(0, 0, 4, 0x1);
		reader.BaseStream.Position = 0x40;
		data.currentPattern = 0;
		m_LastVol = new int[4];

		bool eof = false;
		while (reader.BaseStream.Position < reader.BaseStream.Length && !eof)
		{
			byte cmd = reader.ReadByte();
			switch (cmd)
			{
				case 0x50:
					byte val = reader.ReadByte();
					ParsePSGData(val);
					break;
				case 0x61:
					int inc = Mathf.FloorToInt(reader.ReadUInt16() / 735.0f);
					m_CurrRow += inc;
					if (m_CurrRow >= data.patternLength)
					{
						m_CurrRow -= data.patternLength;
						data.AddPatternLine();
					}
					break;
				case 0x62:
				case 0x63:
					m_CurrRow++;
					if (m_CurrRow >= data.patternLength)
					{
						m_CurrRow = 0;
						data.AddPatternLine();
					}
					break;
				case 0x66:
					eof = true;
					break;
			}
		}
		
		matrix.UpdateMatrix();
		view.UpdatePatternData();
	}

	private int m_CurrReg;
	private int m_CurrType;
	private int m_CurrFreq;
	private bool m_NoiseCH2;
	private int m_NoiseMode;
	private int[] m_LastVol;
	
	private void ParsePSGData(byte data)
	{
		bool first = (data & 128) != 0;
		if (first)
		{
			m_CurrReg = (data >> 5) & 3;
			m_CurrType = (data >> 4) & 1;
		}

		if (m_CurrType != 0)
		{
			m_LastVol[m_CurrReg] = 0x0f - (data & 0x0f);
			this.data.SetData(m_CurrReg, m_CurrRow, 2, m_LastVol[m_CurrReg]);
		}
		else if (first && m_CurrReg == 3)
		{
			//set noise
			int noise = (data & 7);
			int nf = noise & 3;
			m_NoiseCH2 = nf == 3;
			m_NoiseMode = (m_NoiseCH2 ? 1 : 0);
			m_NoiseMode |= ((noise >> 2) & 1) << 4;

			if (!m_NoiseCH2)
			{
				this.data.SetData(3, m_CurrRow, 3, 0x20);
				this.data.SetData(3, m_CurrRow, 4, m_NoiseMode);
				this.data.SetData(3, m_CurrRow, 0, (int)VirtualKeyboard.EncodeNoteInfo(1 + nf, 3));
				this.data.SetData(3, m_CurrRow, 1, 1);
				this.data.SetData(m_CurrReg, m_CurrRow, 2, m_LastVol[3]);
			}
		}
		else if (first)
		{
			m_CurrFreq = (data & 0x0f);
		}
		else
		{
			m_CurrFreq = (m_CurrFreq & 0x0f) | ((data & 0x3f) << 4);
			byte noteData = GetEncodedNoteData(m_CurrFreq);
			if (m_CurrReg != 2 || !m_NoiseCH2)
			{
				this.data.SetData(m_CurrReg, m_CurrRow, 0, noteData);
				this.data.SetData(m_CurrReg, m_CurrRow, 1, 0);
				this.data.SetData(m_CurrReg, m_CurrRow, 2, m_LastVol[m_CurrReg]);
			}
			else
			{
				this.data.SetData(3, m_CurrRow, 3, 0x20);
				this.data.SetData(3, m_CurrRow, 4, m_NoiseMode);
				this.data.SetData(3, m_CurrRow, 0, noteData);
				this.data.SetData(3, m_CurrRow, 1, 1);
				this.data.SetData(3, m_CurrRow, 2, m_LastVol[3]);
			}
		}
	}

	private byte GetEncodedNoteData(int div)
	{
		if(div == 0)
			return VirtualKeyboard.EncodeNoteInfo(1, 12);
		int invRelativenote;
		int freq = (int)SN76489.Clock.PAL / (2 * div * 16);
		int relNote = Mathf.RoundToInt(Mathf.Log(freq * 440) / Mathf.Log(Mathf.Pow(2, 1f / 12f))) + 12 * 3 + 2;

		int note = (relNote % 12) + 1;
		int octave = relNote / 12;

		return VirtualKeyboard.EncodeNoteInfo(note, octave);
	}
}
