//--------------------------------------------------------------------
//
// This is a Unity behaviour script that demonstrates how to capture
// FMOD's mixed audio output by creating a custom DSP and attaching it to
// the master channel group.
//
// The captured audio is then routed to Unity's OnAudioFilterRead callback
// so that it can be recorded by Unity Recorder.
//
// Steps to use:
// 1. Ensure Unity audio is enabled.
// 2. Attach this script to a GameObject that has an active AudioListener.
// 3. Ensure FMOD and Unity use the same sample rate and channel format (Mono or Stereo only).
//    Unity Recorder does not support channel formats above stereo (e.g., 5.1, 7.1).
//
// NOTE: In Editor Play (not recording) you may hear double monitoring:
// FMOD pass-through + Unity OnAudioFilterRead copy. This is expected.
// To avoid: temporarily mute the FMOD Master or route this GameObject to a silent Mixer.
// With Unity Recorder, the Listener is usually muted so double monitoring won't occur.
//
// This document assumes familiarity with Unity scripting. See
// https://unity3d.com/learn/tutorials/topics/scripting for resources
// on learning Unity scripting.
//
//--------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
class FmodAndUnityRecorderBridge : MonoBehaviour
{
    private FMOD.DSP_READ_CALLBACK mReadCallback;
    private FMOD.DSP mCaptureDSP;
    private GCHandle mObjHandle;
    [Tooltip("Wait this many buffers before feeding Unity to avoid start noise")]
    [SerializeField, Min(0)]
    private int latency = 20;
    private int mFrontBufferPosition = 0;
    private Queue<float[]> mFullBufferQueue = new();
    private Queue<float[]> mEmptyBufferQueue = new();
    private readonly object lockOb = new();

    void Start()
    {
        // Prevent FMOD DSP initialization when not in Play Mode.
        if (!Application.isPlaying) return;
        // Validate Unity and FMOD audio config match
        var config = AudioSettings.GetConfiguration();
        int unitySampleRate = config.sampleRate;
        int unityChannels = config.speakerMode == AudioSpeakerMode.Stereo ? 2 : (int)config.speakerMode;
        int fmodSampleRate;

        FMOD.SPEAKERMODE fmodSpeakerMode;
        FMODUnity.RuntimeManager.CoreSystem.getSoftwareFormat(out fmodSampleRate, out fmodSpeakerMode, out _);

        int fmodChannels = fmodSpeakerMode == FMOD.SPEAKERMODE.STEREO ? 2 :
                           fmodSpeakerMode == FMOD.SPEAKERMODE.MONO ? 1 :
                           0; // Default to 0 for unsupported speaker modes(e.g. Surround)

        string unityFormat = unityChannels == 1 ? "Mono" :
                             unityChannels == 2 ? "Stereo" : "Unsupported";

        string fmodFormat = fmodChannels == 1 ? "Mono" :
                            fmodChannels == 2 ? "Stereo" : "Unsupported";

        if (fmodSampleRate != unitySampleRate || fmodChannels != unityChannels)
        {
            Debug.LogError($"FMOD/Unity audio mismatch or unsupported channel format. Unity: {unitySampleRate}Hz/{unityFormat}, FMOD: {fmodSampleRate}Hz/{fmodFormat}\n" +
                           $"Please ensure FMOD and Unity use the same sample rate and channel layout (Mono or Stereo only).");
            enabled = false;
            return;
        }
        mReadCallback = CaptureDSPReadCallback;
        mObjHandle = GCHandle.Alloc(this);
        var desc = new FMOD.DSP_DESCRIPTION
        {
            numinputbuffers = 1,
            numoutputbuffers = 1,
            read = mReadCallback,
            userdata = GCHandle.ToIntPtr(mObjHandle)
        };

        // Attach custom DSP to master channel group
        if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var masterCG) == FMOD.RESULT.OK)
        {
            if (FMODUnity.RuntimeManager.CoreSystem.createDSP(ref desc, out mCaptureDSP) == FMOD.RESULT.OK)
            {
                if (masterCG.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.TAIL, mCaptureDSP) == FMOD.RESULT.OK)
                {
                    mCaptureDSP.setChannelFormat(FMOD.CHANNELMASK.STEREO, 2, FMOD.SPEAKERMODE.STEREO);
                }
                else
                {
                    Debug.LogWarning("FMOD: Failed to add DSP to master channel group.");
                }
            }
            else
            {
                Debug.LogWarning("FMOD: Failed to create DSP.");
            }
        }
        else
        {
            Debug.LogWarning("FMOD: Failed to retrieve master channel group.");
        }
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.DSP_READ_CALLBACK))]
    static FMOD.RESULT CaptureDSPReadCallback(ref FMOD.DSP_STATE dsp_state, IntPtr inbuffer, IntPtr outbuffer, uint length, int inchannels, ref int outchannels)
    {
        // 1. Marshal the raw pointer into the usable C# functions struct
        FMOD.DSP_STATE_FUNCTIONS functions = (FMOD.DSP_STATE_FUNCTIONS)Marshal.PtrToStructure(dsp_state.functions, typeof(FMOD.DSP_STATE_FUNCTIONS));

        // 2. Now you can safely call getuserdata
        functions.getuserdata(ref dsp_state, out IntPtr userData);

        var objHandle = GCHandle.FromIntPtr(userData);
        var obj = objHandle.Target as FmodAndUnityRecorderBridge;
        int lengthElements = (int)length * inchannels;
        float[] buffer;

        // Try to reuse a managed buffer of the exact size to reduce GC pressure.
        lock (obj.lockOb)
        {
            if (obj.mEmptyBufferQueue.Count > 0)
            {
                var tmp = obj.mEmptyBufferQueue.Dequeue();
                buffer = (tmp.Length == lengthElements) ? tmp : new float[lengthElements];
            }
            else
            {
                buffer = new float[lengthElements];
            }
        }

        Marshal.Copy(inbuffer, buffer, 0, lengthElements);

        lock (obj.lockOb)
        {
            obj.mFullBufferQueue.Enqueue(buffer);
        }

        // Pass through to FMOD downstream (so monitoring still works)
        Marshal.Copy(buffer, 0, outbuffer, lengthElements);
        outchannels = inchannels;
        return FMOD.RESULT.OK;
    }

    void OnDestroy()
    {
        if (!Application.isPlaying) return;

        if (mObjHandle.IsAllocated)
        {
            if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var masterCG) == FMOD.RESULT.OK && mCaptureDSP.hasHandle())
            {
                masterCG.removeDSP(mCaptureDSP);
                mCaptureDSP.release();
            }
            mObjHandle.Free();
        }

        lock (lockOb)
        {
            mFullBufferQueue.Clear();
            mEmptyBufferQueue.Clear();
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        // Avoid leftover noise
        Array.Clear(data, 0, data.Length);

        lock (lockOb)
        {
            // Wait for a few captured blocks to avoid initial glitches/pops.
            if (mFullBufferQueue.Count > latency)
            {
                int offset = 0;
                while (mFullBufferQueue.Count > 0 && offset < data.Length)
                {
                    float[] front = mFullBufferQueue.Peek();

                    int remainingInFront = front.Length - mFrontBufferPosition;

                    if (remainingInFront <= 0)
                    {
                        mFullBufferQueue.Dequeue();
                        mFrontBufferPosition = 0;
                        continue;
                    }

                    int remainingInData = data.Length - offset;

                    int copyLength = Math.Min(remainingInFront, remainingInData);
                    Array.Copy(front, mFrontBufferPosition, data, offset, copyLength);

                    mFrontBufferPosition += copyLength;
                    offset += copyLength;

                    // If buffer fully consumed, recycle it
                    if (mFrontBufferPosition >= front.Length)
                    {
                        mFullBufferQueue.Dequeue();
                        mFrontBufferPosition = 0;

                        // Recycle consumed buffers, limit to 32 stored
                        if (mEmptyBufferQueue.Count < 32)
                        {
                            mEmptyBufferQueue.Enqueue(front);
                        }
                    }

                }
            }
        }
    }
}
#endif