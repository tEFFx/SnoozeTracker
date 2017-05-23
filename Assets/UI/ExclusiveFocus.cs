using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExclusiveFocus : MonoBehaviour {
	public static ExclusiveFocus currentFocus;
	public static bool hasFocus { get { return currentFocus != null; } }
	public InputField input { get { return GetComponent<InputField>(); } }
	
	void Update () {
		if (input.isFocused && currentFocus != this) {
			currentFocus = this;
		}else if (!input.isFocused && currentFocus == this) {
			currentFocus = null;
		}
	}
}
