using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

public static class InstallValidationPackage
{
    static AddRequest request;
    static double deadline;
    public static void Install()
    {
        var tarball = Environment.GetEnvironmentVariable("CHARACTER_PACKAGE_TARBALL");
        if (!File.Exists(tarball)) throw new FileNotFoundException(tarball);
        request = Client.Add("file:" + tarball.Replace('\\','/'));
        deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.update += Poll;
    }
    static void Poll()
    {
        if (request.IsCompleted)
        {
            if (request.Status == StatusCode.Success)
                File.WriteAllText("install-result.txt", request.Result.packageId);
            else File.WriteAllText("install-error.txt", request.Error.message);
            EditorApplication.Exit(request.Status == StatusCode.Success ? 0 : 1);
        }
        else if (EditorApplication.timeSinceStartup > deadline) EditorApplication.Exit(2);
    }
}
