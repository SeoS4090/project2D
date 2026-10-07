using System;
using UnityEditor;
using UnityEngine;

public sealed class GoogleSheetDtoGenerator : EditorWindow
{
    private string sheetUrl = GameDataSheetSync.GameSheetUrl;
    private string sheetName = "CommonInstant";
    private string status;
    private bool busy;

    [MenuItem("Tools/Game Data/Generate DTO from Google Sheet")]
    private static void Open() => GetWindow<GoogleSheetDtoGenerator>("Game Data Sheets");

    private void OnGUI()
    {
        sheetUrl = EditorGUILayout.TextField("Sheet URL", sheetUrl);
        sheetName = EditorGUILayout.TextField("Tab", sheetName);
        EditorGUILayout.LabelField("Output", GameDataSheetSync.GameDataFolder);
        using (new EditorGUI.DisabledScope(busy))
        {
            if (GUILayout.Button("Import Connected Sheets Snapshot"))
            {
                try { GameDataSheetSync.ImportConnectedSnapshot(); status = GameDataSheetSync.ValidateLocal(); }
                catch (Exception exception) { status = exception.Message; }
            }
            if (GUILayout.Button("Sync Published Table JSON")) SyncTable();
            if (GUILayout.Button("Sync All Published Game Data + Terms")) SyncAll();
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
    }

    private async void SyncTable()
    {
        busy = true;
        try { await GameDataSheetSync.SyncGameTableAsync(sheetUrl, sheetName); status = GameDataSheetSync.ValidateLocal(); }
        catch (Exception exception) { status = exception.Message; Debug.LogException(exception); }
        finally { busy = false; Repaint(); }
    }

    private async void SyncAll()
    {
        busy = true;
        try { await GameDataSheetSync.SyncAllAsync(); status = GameDataSheetSync.ValidateLocal(); }
        catch (Exception exception) { status = exception.Message; Debug.LogException(exception); }
        finally { busy = false; Repaint(); }
    }
}
