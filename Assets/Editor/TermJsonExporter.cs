using System;
using UnityEditor;
using UnityEngine;

public sealed class TermJsonExporter : EditorWindow
{
    private string sheetUrl = GameDataSheetSync.TermSheetUrl;
    private string tabName = "term";
    private string status;
    private bool busy;

    [MenuItem("Tools/Localization/Export Terms to JSON")]
    private static void Open() => GetWindow<TermJsonExporter>("Term JSON Exporter");

    private void OnGUI()
    {
        sheetUrl = EditorGUILayout.TextField("Sheet URL", sheetUrl);
        tabName = EditorGUILayout.TextField("Tab", tabName);
        EditorGUILayout.LabelField("Output", GameDataSheetSync.TermFolder);
        using (new EditorGUI.DisabledScope(busy))
            if (GUILayout.Button("Sync Published Term JSON")) Export();
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
    }

    private async void Export()
    {
        busy = true;
        try { await GameDataSheetSync.SyncTermsAsync(sheetUrl, tabName); status = GameDataSheetSync.ValidateLocal(); }
        catch (Exception exception) { status = exception.Message; Debug.LogException(exception); }
        finally { busy = false; Repaint(); }
    }
}
