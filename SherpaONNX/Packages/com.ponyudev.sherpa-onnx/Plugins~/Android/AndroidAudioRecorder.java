package com.ponyudev.sherpaonnx.audio;

import android.content.Context;
import android.media.AudioFormat;
import android.media.AudioManager;
import android.media.AudioRecord;
import android.media.AudioRecordingConfiguration;
import android.media.MediaRecorder;
import android.os.Build;
import android.util.Log;

import java.util.List;

/**
 * Native Android AudioRecord wrapper for Unity.
 * Captures 16 kHz mono PCM16 audio on a background thread,
 * converts to float [-1..1] before passing to C#.
 * Called from C# via AndroidJavaObject.
 *
 * Uses PCM_16BIT because many Android devices return silence
 * when using PCM_FLOAT with AudioRecord.
 */
public class AndroidAudioRecorder
{
    private static final String TAG = "SherpaOnnxMic";
    private static final int SAMPLE_RATE = 16000;
    private static final int CHANNEL = AudioFormat.CHANNEL_IN_MONO;
    private static final int ENCODING = AudioFormat.ENCODING_PCM_16BIT;
    private static final int CHUNK_SIZE = 1600; // 100 ms at 16 kHz

    private AudioRecord _recorder;
    private Thread _thread;
    private volatile boolean _isRecording;

    private float[] _writeBuffer;
    private int _writePos;
    private final Object _lock = new Object();

    private volatile float _lastChunkMaxAbs;
    private volatile int _lastChunkRead;
    private volatile int _totalChunksRead;

    public boolean start()
    {
        int minBuf = AudioRecord.getMinBufferSize(
            SAMPLE_RATE, CHANNEL, ENCODING);

        if (minBuf == AudioRecord.ERROR
            || minBuf == AudioRecord.ERROR_BAD_VALUE)
        {
            Log.e(TAG, "getMinBufferSize failed: " + minBuf);
            return false;
        }

        int bufSize = Math.max(minBuf * 2, SAMPLE_RATE * 2);

        try
        {
            _recorder = new AudioRecord(
                MediaRecorder.AudioSource.MIC,
                SAMPLE_RATE, CHANNEL, ENCODING,
                bufSize);
        }
        catch (Exception ex)
        {
            Log.e(TAG, "AudioRecord ctor failed: " + ex.getMessage());
            return false;
        }

        if (_recorder.getState() != AudioRecord.STATE_INITIALIZED)
        {
            Log.e(TAG, "AudioRecord not initialized.");
            _recorder.release();
            _recorder = null;
            return false;
        }

        synchronized (_lock)
        {
            _writeBuffer = new float[SAMPLE_RATE];
            _writePos = 0;
        }

        _isRecording = true;
        _recorder.startRecording();

        _thread = new Thread(new RecordRunnable(), "SherpaOnnx-AudioRecord");
        _thread.start();

        Log.i(TAG, "Started. sampleRate=" + SAMPLE_RATE
            + " bufSize=" + bufSize
            + " encoding=PCM_16BIT source=MIC");
        return true;
    }

    public void stop()
    {
        _isRecording = false;

        if (_thread != null)
        {
            try { _thread.join(2000); }
            catch (InterruptedException ignored) { }
            _thread = null;
        }

        if (_recorder != null)
        {
            try
            {
                _recorder.stop();
                _recorder.release();
            }
            catch (Exception ex)
            {
                Log.w(TAG, "stop/release error: " + ex.getMessage());
            }
            _recorder = null;
        }

        Log.i(TAG, "Stopped.");
    }

    /**
     * Returns accumulated samples and resets the internal buffer.
     * Called from C# main thread once per frame.
     * Returns null when no new data is available.
     */
    public float[] drainBuffer()
    {
        synchronized (_lock)
        {
            if (_writePos == 0)
                return null;

            float[] result = new float[_writePos];
            System.arraycopy(_writeBuffer, 0, result, 0, _writePos);
            _writePos = 0;
            return result;
        }
    }

    public boolean isRecording() { return _isRecording; }
    public int getSampleRate() { return SAMPLE_RATE; }
    public float getLastChunkMaxAbs() { return _lastChunkMaxAbs; }
    public int getLastChunkRead() { return _lastChunkRead; }
    public int getTotalChunksRead() { return _totalChunksRead; }

    // ── Silence diagnostics ──

    /**
     * Checks all known causes of microphone silence on Android.
     * Returns a semicolon-separated diagnostic string.
     * Requires the Unity Activity context as parameter.
     */
    public String diagnoseSilence(Context context)
    {
        StringBuilder sb = new StringBuilder();

        // 1. Global mic privacy toggle (Android 12+, via reflection)
        if (Build.VERSION.SDK_INT >= 31)
        {
            try
            {
                Object spm = context.getSystemService("sensor_privacy");
                if (spm != null)
                {
                    // SensorPrivacyManager.Sensors.MICROPHONE = 1
                    java.lang.reflect.Method supported =
                        spm.getClass().getMethod(
                            "supportsSensorToggle", int.class);
                    boolean canCheck = (boolean) supported.invoke(spm, 1);
                    if (canCheck)
                    {
                        java.lang.reflect.Method isEnabled =
                            spm.getClass().getMethod(
                                "isSensorPrivacyEnabled", int.class);
                        boolean blocked =
                            (boolean) isEnabled.invoke(spm, 1);
                        sb.append("GlobalMicToggle=")
                          .append(blocked ? "BLOCKED" : "OK")
                          .append("; ");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.append("GlobalMicToggle=ERROR(")
                  .append(ex.getMessage()).append("); ");
            }
        }

        // 2. AudioManager mic mute
        try
        {
            AudioManager am = (AudioManager)
                context.getSystemService(Context.AUDIO_SERVICE);
            if (am != null)
            {
                sb.append("MicMute=")
                  .append(am.isMicrophoneMute())
                  .append("; ");
            }
        }
        catch (Exception ex)
        {
            sb.append("MicMute=ERROR; ");
        }

        // 3. AppOpsManager (Samsung per-app permission)
        if (Build.VERSION.SDK_INT >= 19)
        {
            try
            {
                android.app.AppOpsManager appOps = (android.app.AppOpsManager)
                    context.getSystemService(Context.APP_OPS_SERVICE);
                int mode = appOps.checkOpNoThrow(
                    android.app.AppOpsManager.OPSTR_RECORD_AUDIO,
                    android.os.Process.myUid(),
                    context.getPackageName());
                String modeStr;
                switch (mode)
                {
                    case android.app.AppOpsManager.MODE_ALLOWED:
                        modeStr = "ALLOWED"; break;
                    case android.app.AppOpsManager.MODE_IGNORED:
                        modeStr = "IGNORED"; break;
                    case android.app.AppOpsManager.MODE_ERRORED:
                        modeStr = "ERRORED"; break;
                    default:
                        modeStr = "UNKNOWN(" + mode + ")"; break;
                }
                sb.append("AppOps=").append(modeStr).append("; ");
            }
            catch (Exception ex)
            {
                sb.append("AppOps=ERROR; ");
            }
        }

        // 4. Standard permission check
        try
        {
            boolean granted = context.checkSelfPermission(
                android.Manifest.permission.RECORD_AUDIO)
                == android.content.pm.PackageManager.PERMISSION_GRANTED;
            sb.append("Permission=")
              .append(granted ? "GRANTED" : "DENIED")
              .append("; ");
        }
        catch (Exception ex)
        {
            sb.append("Permission=ERROR; ");
        }

        // 5. Recording silenced by concurrent capture (Android 10+)
        if (Build.VERSION.SDK_INT >= 29)
        {
            try
            {
                AudioManager am = (AudioManager)
                    context.getSystemService(Context.AUDIO_SERVICE);
                if (am != null)
                {
                    List<AudioRecordingConfiguration> configs =
                        am.getActiveRecordingConfigurations();
                    sb.append("ActiveRecordings=")
                      .append(configs.size()).append("; ");
                    for (AudioRecordingConfiguration cfg : configs)
                    {
                        sb.append("Silenced=")
                          .append(cfg.isClientSilenced())
                          .append("; ");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.append("ActiveRecordings=ERROR; ");
            }
        }

        // 6. Device info
        sb.append("Device=").append(Build.MANUFACTURER)
          .append("/").append(Build.MODEL)
          .append(" API=").append(Build.VERSION.SDK_INT);

        return sb.toString();
    }

    // ── Record thread ──

    private class RecordRunnable implements Runnable
    {
        @Override
        public void run()
        {
            short[] shortChunk = new short[CHUNK_SIZE];
            float[] floatChunk = new float[CHUNK_SIZE];
            int diagCount = 0;

            while (_isRecording)
            {
                int read = _recorder.read(
                    shortChunk, 0, shortChunk.length);

                if (read > 0)
                {
                    float maxAbs = 0f;
                    for (int i = 0; i < read; i++)
                    {
                        floatChunk[i] = shortChunk[i] / 32768f;
                        float abs = floatChunk[i] < 0
                            ? -floatChunk[i] : floatChunk[i];
                        if (abs > maxAbs) maxAbs = abs;
                    }

                    _lastChunkMaxAbs = maxAbs;
                    _lastChunkRead = read;
                    _totalChunksRead++;

                    if (diagCount < 10)
                    {
                        Log.i(TAG, "chunk#" + diagCount
                            + " read=" + read
                            + " maxAbs=" + maxAbs);
                        diagCount++;
                    }

                    synchronized (_lock)
                    {
                        ensureCapacity(read);
                        System.arraycopy(
                            floatChunk, 0, _writeBuffer, _writePos, read);
                        _writePos += read;
                    }
                }
                else if (read < 0)
                {
                    Log.w(TAG, "AudioRecord.read error: " + read);
                }
            }
        }
    }

    private void ensureCapacity(int additional)
    {
        int required = _writePos + additional;
        if (required <= _writeBuffer.length)
            return;

        int newSize = Math.max(_writeBuffer.length * 2, required);
        float[] grown = new float[newSize];
        System.arraycopy(_writeBuffer, 0, grown, 0, _writePos);
        _writeBuffer = grown;
    }
}
