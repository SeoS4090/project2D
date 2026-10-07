using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AddressableJsonLoader
{
    // Invoke from Unity's main thread. Asset access and handle release stay on that context.
    public static async Task<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var handle = Addressables.LoadAssetAsync<TextAsset>(address);
        try
        {
            // Polling also works on WebGL, where AsyncOperationHandle.Task is unavailable.
            while (!handle.IsDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                throw new InvalidOperationException($"Cannot load Addressable JSON '{address}'.", handle.OperationException);

            string json = handle.Result.text;
            return await ParseAsync<T>(json, cancellationToken);
        }
        finally
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
    }

    public static async Task<T> ParseAsync<T>(string json, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if UNITY_WEBGL && !UNITY_EDITOR
        await Task.Yield();
        T result = Parse<T>(json);
#else
        T result = await Task.Run(() => Parse<T>(json), cancellationToken);
#endif
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private static T Parse<T>(string json)
    {
        T result = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            MissingMemberHandling = MissingMemberHandling.Error,
            Culture = System.Globalization.CultureInfo.InvariantCulture
        });
        if (result == null)
            throw new JsonSerializationException($"JSON for {typeof(T).Name} cannot be null.");
        if (result is IDTO table)
            table.MakeDictionary();
        return result;
    }
}
