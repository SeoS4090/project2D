using UnityEngine;

public sealed class TrainingGroundHud : MonoBehaviour
{
    [SerializeField] private TrainingPlayerController player;
    [SerializeField] private Transform playerMarker;
    [SerializeField] private Transform dummyMarker;
    [SerializeField] private TrainingDummy dummy;
    [SerializeField] private Sprite playerMapIcon;
    [SerializeField] private Sprite dummyMapIcon;
    [SerializeField] private Sprite panelSprite;

    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private GUIStyle helpStyle;

    public void Bind(TrainingPlayerController controller, TrainingDummy target)
    {
        player = controller;
        dummy = target;
        playerMarker = controller != null ? controller.transform : null;
        dummyMarker = target != null ? target.transform : null;
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawPanel(new Rect(18, 18, 300, 206));
        GUI.Label(new Rect(36, 30, 270, 30), "훈련장  /  COMBAT LAB", titleStyle);
        DrawStat(36, 72, "누적 피해", TrainingGroundSession.Instance != null ? $"{TrainingGroundSession.Instance.TotalDamage:0}" : "0");
        DrawStat(36, 101, "최대 피해", TrainingGroundSession.Instance != null ? $"{TrainingGroundSession.Instance.HighestHit:0}" : "0");
        DrawStat(36, 130, "유효 타격", TrainingGroundSession.Instance != null ? $"{TrainingGroundSession.Instance.HitCount}" : "0");
        DrawStat(36, 159, "훈련 시간", TrainingGroundSession.Instance != null ? FormatTime(TrainingGroundSession.Instance.SessionSeconds) : "00:00");

        GUI.Label(new Rect(36, 195, 270, 20), "WASD 이동   ·   마우스 조준   ·   좌클릭 공격", helpStyle);
        GUI.Label(new Rect(36, 217, 270, 20), "SPACE 대시   ·   R 시험 초기화", helpStyle);

        DrawDashCharges();
        DrawDummyHealth();
        DrawMinimap();
        DrawRecentEvents();
    }

    private void DrawStat(float x, float y, string label, string value)
    {
        GUI.Label(new Rect(x, y, 130, 22), label, labelStyle);
        GUI.Label(new Rect(x + 150, y, 118, 22), value, valueStyle);
    }

    private void DrawDashCharges()
    {
        DrawPanel(new Rect(18, 252, 300, 78));
        GUI.Label(new Rect(36, 263, 255, 20), "대시 충전", labelStyle);
        if (player == null) return;
        for (int i = 0; i < player.MaxDashCharges; i++)
        {
            Rect charge = new Rect(36 + i * 38, 289, 28, 13);
            DrawRect(charge, i < player.DashCharges ? new Color(0.27f, 0.82f, 0.78f) : new Color(0.16f, 0.22f, 0.26f));
        }
        if (player.DashCharges < player.MaxDashCharges)
            GUI.Label(new Rect(126, 283, 150, 24), "다음 충전 진행 중", helpStyle);
    }

    private void DrawDummyHealth()
    {
        DrawPanel(new Rect(18, 344, 300, 78));
        GUI.Label(new Rect(36, 356, 255, 20), "허수아비 내구도", labelStyle);
        float fraction = dummy != null ? dummy.Health / Mathf.Max(1f, dummy.MaxHealth) : 0f;
        DrawRect(new Rect(36, 385, 252, 12), new Color(0.14f, 0.2f, 0.23f));
        DrawRect(new Rect(36, 385, 252 * fraction, 12), new Color(0.91f, 0.55f, 0.3f));
        GUI.Label(new Rect(36, 400, 252, 18), dummy != null ? $"{dummy.Health:0} / {dummy.MaxHealth:0}" : "대상 없음", helpStyle);
    }

    private void DrawMinimap()
    {
        const float size = 170f;
        Rect panel = new Rect(Screen.width - size - 20f, 20f, size, size);
        DrawPanel(panel);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, size - 24f, 20f), "훈련장 지도", labelStyle);
        Rect map = new Rect(panel.x + 16f, panel.y + 38f, size - 32f, size - 54f);
        DrawRect(map, new Color(0.12f, 0.2f, 0.21f));
        DrawRect(new Rect(map.x, map.y, map.width, 3), new Color(0.29f, 0.43f, 0.4f));
        DrawRect(new Rect(map.x, map.yMax - 3, map.width, 3), new Color(0.29f, 0.43f, 0.4f));
        DrawRect(new Rect(map.x, map.y, 3, map.height), new Color(0.29f, 0.43f, 0.4f));
        DrawRect(new Rect(map.xMax - 3, map.y, 3, map.height), new Color(0.29f, 0.43f, 0.4f));
        DrawMapMarker(map, playerMarker, playerMapIcon, new Color(0.38f, 0.9f, 0.79f));
        DrawMapMarker(map, dummyMarker, dummyMapIcon, new Color(1f, 0.68f, 0.34f));
    }

    private void DrawMapMarker(Rect map, Transform marker, Sprite icon, Color color)
    {
        if (marker == null) return;
        Vector2 normalized = new Vector2(Mathf.InverseLerp(-12f, 12f, marker.position.x), Mathf.InverseLerp(-7f, 7f, marker.position.y));
        Rect dot = new Rect(map.x + normalized.x * map.width - 6f, map.yMax - normalized.y * map.height - 6f, 12f, 12f);
        if (icon == null) DrawRect(dot, color);
        else
        {
            GUI.color = color;
            GUI.DrawTexture(dot, icon.texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }
    }

    private void DrawRecentEvents()
    {
        TrainingGroundSession session = TrainingGroundSession.Instance;
        if (session == null) return;
        float width = 190f;
        float height = 28f + session.RecentEvents.Count * 20f;
        Rect panel = new Rect(Screen.width - width - 20f, Screen.height - height - 20f, width, height);
        DrawPanel(panel);
        GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, width - 24f, 18f), "최근 전투 기록", labelStyle);
        int index = 0;
        foreach (string entry in session.RecentEvents)
            GUI.Label(new Rect(panel.x + 12f, panel.y + 30f + index++ * 20f, width - 24f, 18f), entry, helpStyle);
    }

    private void DrawPanel(Rect rect)
    {
        if (panelSprite != null)
        {
            DrawNineSlice(rect, panelSprite, Color.white);
            return;
        }
        DrawRect(rect, new Color(0.045f, 0.08f, 0.095f, 0.92f));
        DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(0.28f, 0.67f, 0.62f, 0.95f));
    }

    private static void DrawNineSlice(Rect destination, Sprite sprite, Color tint)
    {
        Rect source = sprite.textureRect;
        Vector4 border = sprite.border;
        float left = Mathf.Min(16f, destination.width * 0.5f);
        float right = left;
        float bottom = Mathf.Min(16f, destination.height * 0.5f);
        float top = bottom;
        float[] dx = { destination.x, destination.x + left, destination.xMax - right, destination.xMax };
        float[] dy = { destination.y, destination.y + bottom, destination.yMax - top, destination.yMax };
        float[] sx = { source.x, source.x + border.x, source.xMax - border.z, source.xMax };
        float[] sy = { source.y, source.y + border.y, source.yMax - border.w, source.yMax };
        float textureWidth = sprite.texture.width;
        float textureHeight = sprite.texture.height;
        GUI.color = tint;
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            Rect target = Rect.MinMaxRect(dx[x], dy[y], dx[x + 1], dy[y + 1]);
            Rect uv = Rect.MinMaxRect(sx[x] / textureWidth, sy[y] / textureHeight, sx[x + 1] / textureWidth, sy[y + 1] / textureHeight);
            GUI.DrawTextureWithTexCoords(target, sprite.texture, uv, true);
        }
        GUI.color = Color.white;
    }

    private void DrawRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.74f, 0.95f, 0.87f) } };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.76f, 0.84f, 0.84f) } };
        valueStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white } };
        helpStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = new Color(0.58f, 0.7f, 0.71f) } };
    }
}
