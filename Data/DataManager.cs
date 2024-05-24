using System;
using System.IO;
using Newtonsoft.Json;
using ProjBobcat.Class.Model;
using ProjBobcat.DefaultComponent.Launch;
using ProjBobcat.Interface;

namespace NewRGB.Data;

public class DataManager
{
    public static DataManager Instance { get; } = new();
    public string DataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".rgbcraft");
    private ILauncherAccountParser? _launcherAccountParser;

    public void InitData(ILauncherAccountParser launcherAccountParser)
    {
        _launcherAccountParser = launcherAccountParser;
        if (!Directory.Exists(DataPath))
        {
            Directory.CreateDirectory(DataPath);
        }
    }

    public ILauncherAccountParser DefaultLauncherAccountParser()
    {
        return new DefaultLauncherAccountParser(DataPath, Guid.Empty);
    }

    private T? Init<T>(string path)
    {
        path = Path.Combine(DataPath, path);
        return File.Exists(path) ? JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) : default;
    }

    public void SaveAll()
    {
    }

    private void Save<T>(T? field, string path)
    {
        path = Path.Combine(DataPath, path);
        if (field != null)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(field));
        }
    }

    public PlayerUUID? ManageAuth(IAuthenticator authenticator)
    {
        var result = authenticator.Auth(false);
        if (result.AuthStatus is AuthStatus.Failed or AuthStatus.Unknown || result.SelectedProfile == null)
        {
            return null;
        }
        return result.SelectedProfile.UUID;
    }

    public bool HasAccount()
    {
        return _launcherAccountParser is { LauncherAccount.ActiveAccountLocalId: not null };
    }
}