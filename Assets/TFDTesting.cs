using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TFDTesting : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if ( Input.GetKeyDown ( KeyCode.F10 ) ) {
            TinyFileDialogs.OpenFileDialog ofd = new TinyFileDialogs.OpenFileDialog ( );
            ofd.title = "Open tune";
            ofd.filterPatterns = new string[] { "*.tfm"};
            ofd.description = "Tunes";
            ofd.defaultPath = UnityEngine.Application.dataPath;

            if ( ofd.ShowDialog ( ) )
                Debug.Log ( "Trying to open " + ofd.filePath );
        }
	}
}
