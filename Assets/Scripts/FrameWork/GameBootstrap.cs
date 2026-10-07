using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootstrap : PersistentSingleton<GameBootstrap>
{
    public bool IsReady { get; private set; }
    public Exception LastError { get; private set; }
    public GameSaveStore Saves { get; private set; }
    public event Action Ready;
    private Task initialization;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static async void StartFramework()
    {
        // The developer-only training scene is deliberately isolated from save and account services.
        if (SceneManager.GetActiveScene().name == "TrainingGround") return;
        try { await GetOrCreate().InitializeAsync(); }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    public Task InitializeAsync()
    {
        if (IsReady) return Task.CompletedTask;
        if (initialization != null && !initialization.IsCompleted) return initialization;
        return initialization = InitializeCoreAsync();
    }

    private async Task InitializeCoreAsync()
    {
        LastError = null;
        try
        {
            var gameData = GameDataDTO.GetOrCreate();
            if (!gameData.IsLoaded)
                await gameData.GameDataLoadAll();
            LifetimeToken.ThrowIfCancellationRequested();
            GamePreferences.Initialize(gameData.commonInstant);
            Saves = new GameSaveStore(System.IO.Path.Combine(Application.persistentDataPath, "Saves"),
                gameData.commonInstant.GetData<int>("default_save_slot"));

            var terms = Term.GetOrCreate();
            await terms.InitializeAsync(gameData.commonInstant.GetData<string>("default_language"),
                gameData.commonInstant.GetData<string>("fallback_language"));
            foreach (string key in gameData.GetRequiredTermKeys())
                if (!terms.TryGetTerm(key, out _))
                    throw new InvalidOperationException($"Game data references missing term '{key}'.");
            LifetimeToken.ThrowIfCancellationRequested();
            IsReady = true;
        }
        catch (Exception exception)
        {
            LastError = exception;
            throw;
        }
        Ready?.Invoke();
    }
}
