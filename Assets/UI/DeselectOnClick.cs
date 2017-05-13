using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeselectOnClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

    private bool m_Hover;

	public void OnPointerClick(PointerEventData evtData) {
        EventSystem.current.SetSelectedGameObject ( null );
    }

    public void OnPointerEnter(PointerEventData evtData) {
        m_Hover = true;
    }

    public void OnPointerExit(PointerEventData evtData) {
        if (!m_Hover)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        m_Hover = true;
    }
}
