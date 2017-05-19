using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickValueNote : ClickValue {

	public override void SetValue(int value) {
		m_Value = value;

		string note = ((VirtualKeyboard.Note) (value % 12 + 1)).ToString();
		note = note.Replace('s', '#');

		textValue.text = prefix + note + ( value / 12 );
	}
}
