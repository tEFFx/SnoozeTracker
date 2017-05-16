using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeselectOnClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
    public static bool enabled;
    private bool m_Hover;

	public void OnPointerClick(PointerEventData evtData) {
        EventSystem.current.SetSelectedGameObject ( null );
    }

    public void OnPointerEnter(PointerEventData evtData) {
        if ( !enabled )
            return;

        m_Hover = true;
    }

    public void OnPointerExit(PointerEventData evtData) {
        if (!m_Hover || !enabled)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        m_Hover = true;
    }
}
