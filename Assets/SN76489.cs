using System.Collections;
using System;

public class SN76489 {
    public enum Clock { NTSC = 3579545, PAL = 3546893 }
    public static readonly float[] VOLUME_TABLE = {
        1.0f,
        0.794328234724281f,
        0.630957344480193f,
        0.501187233627272f,
        0.398107170553497f,
        0.316227766016838f,
        0.251188643150958f,
        0.199526231496888f,
        0.158489319246111f,
        0.125892541179417f,
        0.1f,
        0.0794328234724281f,
        0.0630957344480193f,
        0.0501187233627272f,
        0.0398107170553497f,
        0.0f
    };

    public static readonly int[] NOISE_COUNTER = {
        0x10, 0x20, 0x40
    };

    public static readonly int CLOCK_DIVIDER = 16;
    public static readonly int NOISE_TAPPED = 0x9;
    public static readonly int NOISE_SR_WIDTH = 16;

    public int clock { get { return mClock; } }

    private int mClock;
    private int mSampleRate;
    private float mCyclesPerSample;
    private float mCycleCount;
    private float mInvCycles;

    private int[] mFreq = new int[4];
    private int[] mCount = new int[4];
    public int[] mAttn = new int[4];
    private bool[] mFlipFlop = new bool[4];
    private int mNoiseSR = 0x8000;
    private int mNoiseTap;

    private int mCurrentReg;
    private int mCurrentType;

    public SN76489(int _samplerate, int _clockspeed) {
        mSampleRate = _samplerate;
        mClock = _clockspeed;
        mCyclesPerSample = (float)mClock / (float)CLOCK_DIVIDER / (float)mSampleRate;
        mInvCycles = 1f / mCyclesPerSample;
        mCycleCount = mCyclesPerSample;
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
        } else if ( first ) {
            mFreq[mCurrentReg] = ( mFreq [ mCurrentReg ] & 0x3f0 ) | ( _data & 0x0f );
        } else {
            mFreq[mCurrentReg] = ( mFreq [ mCurrentReg ] & 0x0f ) | (( _data & 0x3f ) << 4);
        }
    }

    public float Render() {
        float output = 0;
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

                        mNoiseSR = ( mNoiseSR >> 1 ) | ( ( fb == 1 ? Parity ( mNoiseSR & NOISE_TAPPED ) : mNoiseSR & 1 ) << (NOISE_SR_WIDTH - 1) );
                        mFlipFlop [ 3 ] = (mNoiseSR & 1) != 0;
                    }
                }

                output += GetVolume(i);
            }

            mCycleCount -= 1.0f;
        }

        mCycleCount += mCyclesPerSample;

        return (output / (float)Math.Ceiling(mCycleCount)) * 0.25f;
    }

    private float GetVolume(int _chn)
    {
        return mFlipFlop[_chn] ? VOLUME_TABLE[mAttn[_chn]] : 0;
    }

    private int Parity(int _val) {
        _val ^= _val >> 8;
        _val ^= _val >> 4;
        _val ^= _val >> 2;
        _val ^= _val >> 1;
        return _val & 1;
    }
}
