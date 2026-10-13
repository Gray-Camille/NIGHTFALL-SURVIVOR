using UnityEngine;

/// <summary>
/// 主菜单与游戏场景之间的交接：菜单里选定存档后写进这里，
/// 游戏场景启动时读出来决定「新建 / 继续」以及用哪个存档位。
/// 两个场景共享的唯一静态状态，避免菜单信息通过 PlayerPrefs 绕一圈。
/// </summary>
public static class MenuFlow
{
    /// <summary>游戏场景名（Build Settings 里必须存在）。</summary>
    public const string GameSceneName = "MainScene";
    /// <summary>主菜单场景名（Build Settings 里必须存在，且排在第一位）。</summary>
    public const string MenuSceneName = "MainMenuScene";

    public static bool HasPending { get; private set; }
    public static bool PendingNewGame { get; private set; }
    public static int PendingSlot { get; private set; }

    /// <summary>菜单里点了某个存档位：登记请求，随后自行加载游戏场景。</summary>
    public static void RequestStart(bool newGame, int slot)
    {
        HasPending = true;
        PendingNewGame = newGame;
        PendingSlot = Mathf.Clamp(slot, 1, GamePersistence.SlotCount);
    }

    /// <summary>游戏场景取走请求（取走即清空，避免重进场景时重复开局）。</summary>
    public static void Consume(out bool newGame, out int slot)
    {
        newGame = PendingNewGame;
        slot = PendingSlot < 1 ? 1 : PendingSlot;
        HasPending = false;
    }

    public static void Clear() { HasPending = false; PendingSlot = 0; }
}
