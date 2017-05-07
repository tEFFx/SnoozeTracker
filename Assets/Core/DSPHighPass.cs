using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OutputDSP {
    public OutputDSP(double lpCutoff, double hpCutoff, int sampleRate) {
        double dt = 1f / sampleRate;
        double lpRc = 1f / ( 2 * ( double ) System.Math.PI * lpCutoff );
        double hpRc = 1f / ( 2 * ( double ) System.Math.PI * hpCutoff );
        m_AlphaLP = lpRc / ( lpRc + dt );
        m_AlphaHP = hpRc / ( hpRc + dt );
    }

    private double m_AlphaLP;
    private double m_AlphaHP;

    private double m_LPOut;
    private double m_LPIn;
    private double m_HPOut;
    private double m_HPIn;

    public double Filter(double x) {
        return LowPass(HighPass(x));
    }

    private double LowPass(double x) {
        m_LPOut = m_LPOut + m_AlphaLP * (x - m_LPOut);
        m_LPIn = x;
        return m_LPOut;
    }

    private double HighPass(double x) {
        m_HPOut = m_AlphaHP * ( m_HPOut + x - m_HPIn );
        m_HPIn = x;
        return m_HPOut;
    }
}
