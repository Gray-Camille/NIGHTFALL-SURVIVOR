using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 主菜单场景的入口。只在 MainMenuScene 里搭菜单；游戏场景由 SceneBootstrap 负责，二者互不打扰。
/// 用 sceneLoaded 而不是 RuntimeInitializeOnLoadMethod：后者只在启动时跑一次，
/// 从游戏里按 M 回到主菜单时不会重跑，菜单就会是空的。
/// </summary>
public static class MainMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void HookSceneLoaded()
    {
        // 关闭域重载时静态事件会跨 Play 残留，先减后加保证只订阅一次
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>兜底：确保启动时的第一个场景也被处理一次。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HookFirstScene()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MenuFlow.MenuSceneName) return;
        if (Object.FindObjectOfType<MenuUI>() != null) return;
        // 同一场景被处理两次（事件 + 兜底）时不要重复搭建
        foreach (var root in scene.GetRootGameObjects())
            if (root != null && root.GetComponentInChildren<MenuUI>(true) != null) return;
        new GameObject("MenuUI").AddComponent<MenuUI>();
    }
}
