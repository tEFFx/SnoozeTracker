using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeselectOnClick : MonoBehaviour, IPointerClickHandler {

	public void OnPointerClick(PointerEventData evtData) {
        EventSystem.current.SetSelectedGameObject ( null );
    }
}
