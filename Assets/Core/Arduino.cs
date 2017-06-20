using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

public class Arduino : MonoBehaviour {
	public string port = "/dev/ttyUSB0";
	public int baudRate = 115200;

	private SerialPort m_Port;
	
	// Use this for initialization
	void Start () {
		m_Port = new SerialPort(port, baudRate);
		m_Port.Open();
	}
	
	void OnDestroy() {
		if(m_Port != null)
			m_Port.Close();
	}

	public void WriteByte(byte data) {
		if(m_Port == null)
			return;
		
		m_Port.Write(new byte[]{data}, 0, 1);
	}
}
