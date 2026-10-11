using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Rendering.Universal;

public static class LabLevelPreview
{
    [MenuItem("Tools/Lab/Export Course Previews")]
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var camera = Camera.main;
        var position = camera.transform.position;
        float size = camera.orthographicSize;
        Directory.CreateDirectory("Artifacts");
        Render(camera, "warmup", position, size, 1280, 720);
        Render(camera, "start", position, size, 1280, 720);
        Render(camera, "middle", new Vector3(12, position.y, position.z), size, 1280, 720);
        Render(camera, "finish", new Vector3(14.3f, position.y, position.z), size, 1280, 720);
        Render(camera, "overview", new Vector3(9, 0.5f, -10), 6, 1920, 720);
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var mode = canvas.renderMode;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var panels = canvas.GetComponentsInChildren<RectTransform>(true);
        var over = panels.First(t => t.name == "GameOver").gameObject;
        var play = panels.First(t => t.name == "GamePlay").gameObject;
        over.SetActive(true); play.SetActive(false); Canvas.ForceUpdateCanvases();
        Render(camera, "ending", position, size, 1280, 720);
        over.SetActive(false); play.SetActive(true); canvas.renderMode = mode;
        camera.transform.position = position;
        camera.orthographicSize = size;
        Debug.Log("Course previews exported to Artifacts.");
    }

    static void Render(Camera camera, string name, Vector3 position, float size, int width, int height)
    {
        var target = RenderTexture.GetTemporary(width, height, 24);
        camera.transform.position = position; camera.orthographicSize = size; camera.targetTexture = target;
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
        File.WriteAllBytes("Artifacts/" + name + ".png", texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        RenderTexture.active = previous; camera.targetTexture = null;
        RenderTexture.ReleaseTemporary(target);
    }
}
