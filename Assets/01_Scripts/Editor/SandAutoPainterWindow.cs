using UnityEditor;
using UnityEngine;

public sealed class SandAutoPainterWindow : EditorWindow
{
    private const string DefaultReferencePath = "Assets/03_Art/Reference/WhaleReference.png";
    private const string DefaultMaskPath = "Assets/03_Art/Reference/WhaleTemplateMask.png";

    private enum DebugMode
    {
        AutoPaint,
        WhaleMold,
        WhaleOutline
    }

    private Texture2D _referenceImage;
    private Texture2D _templateMask;
    private DebugMode _mode;
    private int _horizontalResolution = 128;
    private float _dropsPerSecond = 180f;
    private bool _useGamePalette = true;
    private Color _moldColor = new Color(0.86f, 0.76f, 0.58f, 1f);
    private Color _outlineColor = new Color(0.35f, 0.24f, 0.16f, 0.72f);
    private int _outlineThickness = 2;

    [MenuItem("Tools/Sand Color/모래 디버그")]
    private static void OpenWindow()
    {
        SandAutoPainterWindow window = GetWindow<SandAutoPainterWindow>("모래 디버그");
        window.minSize = new Vector2(440f, 340f);
        window.Show();
    }

    private void OnEnable()
    {
        _referenceImage ??= AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultReferencePath);
        _templateMask ??= AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultMaskPath);
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("고래 모래 디버그", EditorStyles.boldLabel);
        _mode = (DebugMode)GUILayout.Toolbar(
            (int)_mode,
            new[] { "자동 그리기", "고래 모래틀", "외곽선 가이드" }
        );
        EditorGUILayout.Space(8f);

        SandSpawner spawner = Application.isPlaying ? FindObjectOfType<SandSpawner>() : null;
        DrawPlayModeStatus(spawner);

        switch (_mode)
        {
            case DebugMode.AutoPaint:
                DrawAutoPaintMode(spawner);
                break;
            case DebugMode.WhaleMold:
                DrawWhaleMoldMode(spawner);
                break;
            case DebugMode.WhaleOutline:
                DrawWhaleOutlineMode(spawner);
                break;
        }
    }

    private static void DrawPlayModeStatus(SandSpawner spawner)
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("게임 씬을 플레이한 상태에서 사용할 수 있습니다.", MessageType.Warning);
        }
        else if (spawner == null)
        {
            EditorGUILayout.HelpBox("게임 씬에서 SandSpawner를 찾을 수 없습니다.", MessageType.Error);
        }
    }

    private void DrawAutoPaintMode(SandSpawner spawner)
    {
        EditorGUILayout.HelpBox(
            "고래 레퍼런스를 아래쪽부터 읽어 실제 모래로 자동으로 그립니다.",
            MessageType.Info
        );
        _referenceImage = (Texture2D)EditorGUILayout.ObjectField(
            "고래 레퍼런스",
            _referenceImage,
            typeof(Texture2D),
            false
        );
        _horizontalResolution = EditorGUILayout.IntSlider("가로 모래 해상도", _horizontalResolution, 32, 320);
        _dropsPerSecond = EditorGUILayout.Slider("초당 낙하 수", _dropsPerSecond, 30f, 180f);
        _useGamePalette = EditorGUILayout.Toggle("게임 모래 팔레트 사용", _useGamePalette);

        if (spawner != null)
        {
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.ProgressBar(
                progressRect,
                spawner.DebugPaintProgress,
                $"진행률 {spawner.DebugPaintProgress * 100f:0.0}%"
            );
        }

        using (new EditorGUI.DisabledScope(spawner == null || _referenceImage == null))
        {
            if (GUILayout.Button("고래 자동 그리기 시작", GUILayout.Height(36f)))
            {
                spawner.StartDebugPainting(
                    ReadPixels(_referenceImage),
                    _referenceImage.width,
                    _referenceImage.height,
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

    private void DrawWhaleMoldMode(SandSpawner spawner)
    {
        EditorGUILayout.HelpBox(
            "고래 실루엣 부분을 비워 둔 모래틀을 만듭니다. 떨어지는 모래는 고래 모양 안에 쌓입니다.",
            MessageType.Info
        );
        DrawTemplateMaskField();
        _moldColor = EditorGUILayout.ColorField("모래틀 색", _moldColor);

        using (new EditorGUI.DisabledScope(spawner == null || _templateMask == null))
        {
            if (GUILayout.Button("고래 모래틀 만들기", GUILayout.Height(36f)))
            {
                Color32[] mask = ReadPixels(_templateMask);
                spawner.DebugBuildTemplateMold(
                    mask,
                    _templateMask.width,
                    _templateMask.height,
                    _moldColor
                );
            }

            DrawClearTemplateButton(spawner);
        }
    }

    private void DrawWhaleOutlineMode(SandSpawner spawner)
    {
        EditorGUILayout.HelpBox(
            "고래 실루엣의 외곽선만 표시합니다. 가이드는 물리에는 영향을 주지 않고 모래 위에 계속 보입니다.",
            MessageType.Info
        );
        DrawTemplateMaskField();
        _outlineColor = EditorGUILayout.ColorField("외곽선 색", _outlineColor);
        _outlineThickness = EditorGUILayout.IntSlider("외곽선 두께", _outlineThickness, 1, 6);

        using (new EditorGUI.DisabledScope(spawner == null || _templateMask == null))
        {
            if (GUILayout.Button("고래 외곽선 가이드 만들기", GUILayout.Height(36f)))
            {
                Color32[] mask = ReadPixels(_templateMask);
                spawner.DebugBuildTemplateOutline(
                    mask,
                    _templateMask.width,
                    _templateMask.height,
                    _outlineColor,
                    _outlineThickness
                );
            }

            DrawClearTemplateButton(spawner);
        }
    }

    private void DrawTemplateMaskField()
    {
        _templateMask = (Texture2D)EditorGUILayout.ObjectField(
            "고래 실루엣 마스크",
            _templateMask,
            typeof(Texture2D),
            false
        );
    }

    private static void DrawClearTemplateButton(SandSpawner spawner)
    {
        if (GUILayout.Button("모래틀 / 가이드 지우기", GUILayout.Height(28f)))
        {
            spawner.DebugClearTemplate();
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
