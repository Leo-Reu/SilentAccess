using UnityEngine;
using UnityEngine.InputSystem;

public static class InputBindingSave
{
    private const string BindingKey = "SilentAccess.InputBindings";

    public static void Load(InputActionAsset actions)
    {
        if (actions == null || !PlayerPrefs.HasKey(BindingKey))
        {
            return;
        }

        string json = PlayerPrefs.GetString(BindingKey);

        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            actions.LoadBindingOverridesFromJson(json);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"키 바인딩 불러오기 실패: {exception.Message}");
        }
    }

    public static void Save(InputActionAsset actions)
    {
        if (actions == null)
        {
            return;
        }

        PlayerPrefs.SetString(BindingKey, actions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(BindingKey);
        PlayerPrefs.Save();
    }
}
