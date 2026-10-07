using System.Threading;
using UnityEngine;

public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
{
    public static T Instance { get; private set; }
    private CancellationTokenSource lifetime;
    private CancellationToken lifetimeToken;
    protected CancellationToken LifetimeToken => lifetimeToken;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = (T)this;
        lifetime = new CancellationTokenSource();
        lifetimeToken = lifetime.Token;
        if (Application.isPlaying)
            DontDestroyOnLoad(gameObject);
    }

    public static T GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        T existing = FindFirstObjectByType<T>();
        return existing != null ? existing : new GameObject(typeof(T).Name).AddComponent<T>();
    }

    protected virtual void OnDestroy()
    {
        lifetime?.Cancel();
        lifetime?.Dispose();
        if (Instance == this)
            Instance = null;
    }
}
