using System.Collections;
using System;

public class SN76489 {
    public enum Clock { NTSC = 3579545, PAL = 3546893 }
    public static readonly double[] VOLUME_TABLE = {
        1.0,
        0.794328234724281,
        0.630957344480193,
        0.501187233627272,
        0.398107170553497,
        0.316227766016838,
        0.251188643150958,
        0.199526231496888,
        0.158489319246111,
        0.125892541179417,
        0.1,
        0.0794328234724281,
        0.0630957344480193,
        0.0501187233627272,
        0.0398107170553497,
        0.0
    };

    public static readonly int[] NOISE_COUNTER = {
        0x10, 0x20, 0x40
    };

    public static readonly int CLOCK_DIVIDER = 16;
    public static readonly int NOISE_TAPPED = 0x9;
    public static readonly int NOISE_SR_WIDTH = 16;

    public int clock { get { return mClock; } }
    public int stereoByte { get { return mStereoByte; } }

    private int mClock;
    private int mSampleRate;
    private double mCyclesPerSample;
    private double mCycleCount;
    private double mInvCycles;

    private int[] mFreq = new int[4];
    private int[] mCount = new int[4];
    private int[] mAttn = new int[4];
    private bool[] mFlipFlop = new bool[4];
    private int mNoiseSR = 0x8000;
    private int mStereoByte = 0xFF;

    private int mCurrentReg;
    private int mCurrentType;

    private double mHPAlpha;
    private double mLHPOut = 0;
    private double mLHPIn = 0;
    private double mRHPOut = 0;
    private double mRHPIn = 0;

    public SN76489(int _samplerate, int _clockspeed) {
        mSampleRate = _samplerate;
        mClock = _clockspeed;
        mCyclesPerSample = ( double ) mClock / ( double ) CLOCK_DIVIDER / ( double ) mSampleRate;
        mInvCycles = 1.0 / mCyclesPerSample;
        mCycleCount = mCyclesPerSample;

        double dt = 1f / _samplerate;
        double hpRc = 1f / ( 2 * ( double ) System.Math.PI * 20 );
        mHPAlpha = hpRc / ( hpRc + dt );
    }

    public void Reset() {
        mFreq = new int[4];
        mCount = new int[4];
        mAttn = new int[4];
        mFlipFlop = new bool[4];
        mNoiseSR = 0x8000;
        mStereoByte = 0xFF;
    }

    public void Write(int _data) {
        bool first = ( _data & 128 ) != 0;
        if (first) {
            mCurrentReg = ( _data >> 5 ) & 3;
            mCurrentType = ( _data >> 4 ) & 1;
        }

        if ( mCurrentType != 0 ) {
            mAttn[mCurrentReg] = _data & 0x0f;
        } else if ( first && mCurrentReg == 3 ) {
            mFreq[3] = _data & 7;
            mNoiseSR = 0x8000;
        } else if ( first ) {
            mFreq[mCurrentReg] = ( mFreq [ mCurrentReg ] & 0x3f0 ) | ( _data & 0x0f );
        } else {
            mFreq[mCurrentReg] = ( mFreq [ mCurrentReg ] & 0x0f ) | (( _data & 0x3f ) << 4);
        }
    }

    public int GetRegister(int register) {
        int reg = ( register >> 1 ) & 3;
        int type = register & 1;

        if ( type == 0 )
            return mFreq [ reg ];

        return mAttn [ reg ];
    }

    public void SetStereo(int channel, bool left, bool right) {
        int leftBit = ( 1 << ( channel + 4 ) );
        int rightBit = ( 1 << channel );

        if ( left )
            mStereoByte |= leftBit;
        else
            mStereoByte &= ~leftBit;

        if ( right )
            mStereoByte |= rightBit;
        else
            mStereoByte &= ~rightBit;
    }

    public void Render(out double left, out double right) {
        left = right = 0;
        int samples = 0;
        while(mCycleCount > 0) {
            for ( int i = 0 ; i < 4; i++ ) {
                mCount [ i ]--;
                if ( mCount [ i ] <= 0 ) {
                    if ( i < 3 ) {
                        mCount [ i ] = mFreq [ i ];
                        if (mFreq[i] > 1)
                            mFlipFlop[i] = !mFlipFlop[i];
                        else if (mFreq[i] == 1)
                            mFlipFlop[i] = true;
                        else
                            mFlipFlop[i] = false;
                    } else {
                        int nf = mFreq [ 3 ] & 3;
                        int fb = ( mFreq [ 3 ] >> 2 ) & 1;
                        mCount [ 3 ] = nf == 3 ? mFreq[2] : (0x10 << nf);

                        mNoiseSR = ( mNoiseSR >> 1 ) | ( ( fb == 1 ? Parity ( mNoiseSR & NOISE_TAPPED ) : mNoiseSR & 1 ) << NOISE_SR_WIDTH );
                        mFlipFlop [ 3 ] = (mNoiseSR & 1) != 0;
                    }
                }

                if ( CheckBit ( mStereoByte, i + 4 ) )
                    left += GetVolume ( i ) * 0.25;

                if ( CheckBit ( mStereoByte, i ) )
                    right += GetVolume ( i ) * 0.25;
            }

            mCycleCount -= 1.0f;
            samples++;
        }

        mCycleCount += mCyclesPerSample;

        left /= samples;
        right /= samples;

        left = HighPass ( left, ref mLHPOut, ref mLHPIn );
        right = HighPass ( right, ref mRHPOut, ref mRHPIn );
    }

    private bool CheckBit(int _byte, int _bit) {
        return ( _byte & ( 1 << _bit ) ) != 0;
    }

    private double GetVolume(int _chn)
    {
        return mFlipFlop[_chn] ? VOLUME_TABLE[mAttn[_chn]] : -VOLUME_TABLE [ mAttn [ _chn ] ];
    }

    private int Parity(int _val) {
        _val ^= _val >> 8;
        _val ^= _val >> 4;
        _val ^= _val >> 2;
        _val ^= _val >> 1;
        return _val & 1;
    }

    private double HighPass(double sample, ref double output, ref double input) {
        output = mHPAlpha * ( output + sample - input );
        input = sample;
        return output;
    }
}
