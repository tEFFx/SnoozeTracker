using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MatrixButton : Button {
    new public Action<int> onClick;

    public override void OnPointerClick(PointerEventData eventData) {
        Press ( (int)eventData.button );
    }

    public override void OnSubmit(BaseEventData eventData) {
        Press ( 0 );

        if ( !IsActive ( ) || !IsInteractable ( ) )
            return;

        DoStateTransition ( SelectionState.Pressed, false );
        StartCoroutine ( OnFinishSubmit ( ) );
    }

    private void Press(int mouseButton) {
        if ( !IsActive ( ) || !IsInteractable ( ) )
            return;

        if ( onClick != null )
            onClick ( mouseButton );
    }

    private IEnumerator OnFinishSubmit() {
        float fadeTime = colors.fadeDuration;
        float startTime = Time.time;

        while ( Time.time - startTime < fadeTime )
            yield return null;

        DoStateTransition ( currentSelectionState, false );
    }
}
