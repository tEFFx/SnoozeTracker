using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midi;

public class MIDIManager : MonoBehaviour {
    public VirtualKeyboard keyboard;
    public bool useVelocity;
    private InputDevice m_Device;
    private List<Pitch> m_Pitches = new List<Pitch>();

    void Start() {
        if ( InputDevice.InstalledDevices.Count > 0 )
            SetMidiDevice ( 0 );
        else
            Debug.Log ( "No MIDI devices detected!" );
    }

    void OnDestroy() {
        CloseDevice ( );
    }

	public void SetMidiDevice(int deviceIndex) {
        m_Pitches.Clear ( );
        CloseDevice ( );
        m_Device = InputDevice.InstalledDevices [ deviceIndex ];
        m_Device.Open ( );
        m_Device.NoteOn += new InputDevice.NoteOnHandler ( NoteStateChange );
        m_Device.StartReceiving ( null );

        Debug.Log ( "Initialized " + m_Device.Name );
    }

    void CloseDevice() {
        if ( m_Device != null )
            m_Device.Close ( );
    }

    void NoteStateChange(NoteOnMessage msg) {
        if ( !m_Pitches.Contains ( msg.Pitch ) ) {
            int vel = useVelocity ? ( int ) ( ( msg.Velocity / 127f ) * 0xF) : 0xF;
            keyboard.SetNoteOn ( PitchToNote ( msg.Pitch ), msg.Pitch.Octave(), vel );
            m_Pitches.Add ( msg.Pitch );
        }else {
            keyboard.SetNoteOff ( PitchToNote ( msg.Pitch ), msg.Pitch.Octave ( ) );
            m_Pitches.Remove ( msg.Pitch );
        }
    }

    VirtualKeyboard.Note PitchToNote(Pitch pitch) {
        return ( VirtualKeyboard.Note ) ( pitch.PositionInOctave ( ) + 1 );
    }
}
