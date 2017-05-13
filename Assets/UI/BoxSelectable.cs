using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BoxSelectable : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler {
    public PatternRow row;
    public int col { get { return transform.GetSiblingIndex(); } }
    public RectTransform rectTransform { get { return GetComponent<RectTransform>(); } }

    public void OnPointerDown(PointerEventData ptrEvt) {
        row.view.boxSelection.StartSelection(this);
    }

    public void OnPointerEnter(PointerEventData ptrEvt) {
        row.view.boxSelection.UpdateSelection(this);
    }
}
