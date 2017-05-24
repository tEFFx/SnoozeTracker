using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExclusiveFocus : MonoBehaviour, ISelectHandler {
	public static ExclusiveFocus currentFocus;
	public static bool hasFocus { get { return currentFocus != null; } }

	void Awake() {
		GetComponent<InputField>().onEndEdit.AddListener(OnEditFinish);
	}
	
	public void OnSelect(BaseEventData evt) {
		currentFocus = this;
	}

	private void OnEditFinish(string val) {
		if (currentFocus == this)
			currentFocus = null;
	}
}
