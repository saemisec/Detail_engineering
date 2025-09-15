using Microsoft.Win32;

public static class AppRegistry
{
    public const string SubKey = @"Software\JSC\DetailENG";
    public const string BaseFolderValue = "BaseFolder";

    public static void Ensure()
    {
        using var key = Registry.CurrentUser.CreateSubKey(SubKey, true);
        if (key.GetValue(BaseFolderValue) == null)
            key.SetValue(BaseFolderValue, "", RegistryValueKind.String);
    }

    public static string GetBaseFolder()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SubKey, false);
        return key?.GetValue(BaseFolderValue, "") as string ?? "";
    }

    public static void SetBaseFolder(string path)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SubKey, true);
        key.SetValue(BaseFolderValue, path ?? "", RegistryValueKind.String);
    }
}
