using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Logos : MonoBehaviour {

    public Sprite logo;
    public Vector2 logoPosition;
    public float scale;

	void OnGUI() {
        if ( logo != null ) {
            Rect logoRect = logo.rect;
            logoRect.position = logoPosition;
            logoRect.width *= scale;
            logoRect.height *= scale;
            GUI.DrawTexture ( logoRect, logo.texture );
        }
    }
}
