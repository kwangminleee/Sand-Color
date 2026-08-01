using UnityEditor;
using UnityEngine;

public sealed class SandAutoPainterWindow : EditorWindow
{
    private const string DefaultImagePath = "Assets/03_Art/Reference/SailboatSandArt.png";

    private Texture2D _sourceImage;
    private int _horizontalResolution = 128;
    private float _dropsPerSecond = 180f;
    private bool _useGamePalette = true;

    [MenuItem("Tools/Sand Color/모래 자동 그리기")]
    private static void OpenWindow()
    {
        SandAutoPainterWindow window = GetWindow<SandAutoPainterWindow>("모래 자동 그리기");
        window.minSize = new Vector2(360f, 310f);
        window.Show();
    }

    private void OnEnable()
    {
        if (_sourceImage == null)
        {
            _sourceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultImagePath);
        }

        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("모래 자동 그리기", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "이미지를 아래쪽 행부터 읽어 실제 플레이처럼 모래를 떨어뜨립니다.",
            MessageType.Info
        );

        _sourceImage = (Texture2D)EditorGUILayout.ObjectField(
            "원본 이미지",
            _sourceImage,
            typeof(Texture2D),
            false
        );
        _horizontalResolution = EditorGUILayout.IntSlider(
            "가로 모래 해상도",
            _horizontalResolution,
            32,
            320
        );
        _dropsPerSecond = EditorGUILayout.Slider(
            "초당 낙하 수",
            _dropsPerSecond,
            30f,
            180f
        );
        _useGamePalette = EditorGUILayout.Toggle("게임 모래 팔레트 사용", _useGamePalette);

        EditorGUILayout.Space(8f);
        SandSpawner spawner = Application.isPlaying
            ? FindObjectOfType<SandSpawner>()
            : null;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("게임 씬을 플레이한 뒤 사용할 수 있습니다.", MessageType.Warning);
        }
        else if (spawner == null)
        {
            EditorGUILayout.HelpBox("게임 씬에서 SandSpawner를 찾을 수 없습니다.", MessageType.Error);
        }
        else
        {
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.ProgressBar(
                progressRect,
                spawner.DebugPaintProgress,
                $"진행률 {spawner.DebugPaintProgress * 100f:0.0}%"
            );
        }

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(
                   !Application.isPlaying || spawner == null || _sourceImage == null))
        {
            if (GUILayout.Button("자동 그리기 시작", GUILayout.Height(36f)))
            {
                spawner.StartDebugPainting(
                    ReadPixels(_sourceImage),
                    _sourceImage.width,
                    _sourceImage.height,
                    _horizontalResolution,
                    _dropsPerSecond,
                    _useGamePalette
                );
            }
        }

        using (new EditorGUI.DisabledScope(spawner == null || !spawner.IsDebugPainting))
        {
            if (GUILayout.Button("그리기 중지", GUILayout.Height(28f)))
            {
                spawner.StopDebugPainting();
            }
        }
    }

    private static Color32[] ReadPixels(Texture2D source)
    {
        RenderTexture temporary = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB
        );
        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(source, temporary);
        RenderTexture.active = temporary;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
        readable.Apply(false, false);
        Color32[] pixels = readable.GetPixels32();

        DestroyImmediate(readable);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);
        return pixels;
    }
}
