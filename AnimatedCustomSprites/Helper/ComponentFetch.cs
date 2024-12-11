using UnityEngine;
using Object = UnityEngine.Object;

namespace ACS.Helper;

public static class ComponentFetch
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
    }

    public static bool TryGetComponent<T>(this GameObject gameObject, out T component) where T : Component
    {
        component = gameObject.GetComponent<T>();
        return component;
    }

    public static bool TryGetComponentInParent<T>(this GameObject gameObject, out T component) where T : Component
    {
        component = gameObject.GetComponentInParent<T>();
        return component;
    }

    public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T component) where T : Component
    {
        component = gameObject.GetComponentInChildren<T>();
        return component;
    }

    public static bool TryFindObject<T>(string name, out T? obj) where T : Object
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<T>()) {
            go.hideFlags = go.hideFlags switch {
                HideFlags.HideAndDontSave => HideFlags.DontSave,
                HideFlags.HideInHierarchy => HideFlags.None,
                _ => go.hideFlags,
            };

            if (go.name != name) {
                continue;
            }

            obj = go;
            return true;
        }

        obj = null;
        return false;
    }
}