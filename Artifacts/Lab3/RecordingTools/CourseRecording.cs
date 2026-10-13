#if UNITY_EDITOR
using System;
using System.IO;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class CourseRecording : IDisposable
{
    readonly MediaEncoder encoder;
    readonly Texture2D image = new Texture2D(640, 360, TextureFormat.RGBA32, false);
    readonly RenderTexture target = new RenderTexture(640, 360, 24);
    readonly string path;
    int frames, samples;
    double energy;
    float peak;

    public CourseRecording(string path)
    {
        this.path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Time.captureFramerate = 120;
        encoder = new MediaEncoder(path,
            new VideoTrackAttributes { frameRate = new MediaRational(120), width = 640, height = 360, includeAlpha = false },
            new AudioTrackAttributes { sampleRate = new MediaRational(AudioSettings.outputSampleRate), channelCount = 2, language = "" });
        if (!AudioRenderer.Start()) throw new Exception("Audio capture could not start.");
    }

    public void Frame()
    {
        var camera = Camera.main;
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (!camera || !canvas) return;
        var mode = canvas.renderMode;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();
        var previous = RenderTexture.active;
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        RenderTexture.active = target;
        image.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
        image.Apply();
        if (!encoder.AddFrame(image)) throw new Exception("Video encoding failed.");
        RenderTexture.active = previous;
        canvas.renderMode = mode;
        using var buffer = new NativeArray<float>(AudioRenderer.GetSampleCountForCaptureFrame() * 2, Allocator.Temp);
        if (!AudioRenderer.Render(buffer)) throw new Exception("Audio rendering failed.");
        if (buffer.Length > 0 && !encoder.AddSamples(buffer)) throw new Exception("Audio encoding failed.");
        foreach (float value in buffer) { energy += value * value; peak = Mathf.Max(peak, Mathf.Abs(value)); }
        samples += buffer.Length;
        frames++;
    }

    public void Dispose()
    {
        AudioRenderer.Stop();
        encoder.Dispose();
        Time.captureFramerate = 0;
        UnityEngine.Object.Destroy(image);
        target.Release();
        UnityEngine.Object.Destroy(target);
        File.WriteAllText(path + ".json", JsonUtility.ToJson(new Evidence { frames = frames, seconds = frames / 120f,
            sampleRate = AudioSettings.outputSampleRate, channels = 2, samples = samples, rms = (float)Math.Sqrt(energy / Math.Max(1, samples)), peak = peak }, true));
    }

    [Serializable] class Evidence { public int frames, sampleRate, channels, samples; public float seconds, rms, peak; }
}
public sealed class RecordingPump : MonoBehaviour
{
    public CourseRecording recording;
    void LateUpdate() { recording?.Frame(); }
}
#endif
