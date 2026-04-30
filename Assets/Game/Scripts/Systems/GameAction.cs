using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameAction
{
    MoveLeft = 0,
    MoveRight = 1,
    Jump = 2,
    Fire = 3,
    Interact = 4,
    Dash = 5,
    Pause = 6,
    Submit = 7,
    Cancel = 8,
    NavigateUp = 9,
    NavigateDown = 10,
    NavigateLeft = 11,
    NavigateRight = 12,
    RangedFire = 13,
    AttackLine1 = 20,
    AttackLine2 = 21,
    AttackLine3 = 22,
    AttackLine4 = 23
}

[Serializable]
public class KeybindingEntry
{
    public GameAction action;
    public string keyCode;
}

public static class GameActionDefaults
{
    private static readonly GameAction[] RebindableActionList =
    {
        GameAction.MoveLeft,
        GameAction.MoveRight,
        GameAction.Jump,
        GameAction.AttackLine1,
        GameAction.AttackLine2,
        GameAction.AttackLine3,
        GameAction.AttackLine4,
        GameAction.RangedFire,
        GameAction.Interact,
        GameAction.Dash,
        GameAction.Pause,
        GameAction.Submit,
        GameAction.Cancel,
        GameAction.NavigateUp,
        GameAction.NavigateDown,
        GameAction.NavigateLeft,
        GameAction.NavigateRight
    };

    public static IEnumerable<GameAction> RebindableActions
    {
        get { return RebindableActionList; }
    }

    public static KeyCode GetDefaultKey(GameAction action)
    {
        switch (action)
        {
            case GameAction.MoveLeft: return KeyCode.LeftArrow;
            case GameAction.MoveRight: return KeyCode.RightArrow;
            case GameAction.Jump: return KeyCode.Space;
            case GameAction.Fire: return KeyCode.LeftControl;
            case GameAction.AttackLine1: return KeyCode.Z;
            case GameAction.AttackLine2: return KeyCode.X;
            case GameAction.AttackLine3: return KeyCode.C;
            case GameAction.AttackLine4: return KeyCode.V;
            case GameAction.RangedFire: return KeyCode.Mouse1;
            case GameAction.Interact: return KeyCode.E;
            case GameAction.Dash: return KeyCode.LeftShift;
            case GameAction.Pause: return KeyCode.Escape;
            case GameAction.Submit: return KeyCode.Return;
            case GameAction.Cancel: return KeyCode.Backspace;
            case GameAction.NavigateUp: return KeyCode.UpArrow;
            case GameAction.NavigateDown: return KeyCode.DownArrow;
            case GameAction.NavigateLeft: return KeyCode.LeftArrow;
            case GameAction.NavigateRight: return KeyCode.RightArrow;
            default: return KeyCode.None;
        }
    }

    public static string GetDisplayName(GameAction action)
    {
        switch (action)
        {
            case GameAction.MoveLeft: return "Mover esquerda";
            case GameAction.MoveRight: return "Mover direita";
            case GameAction.Jump: return "Pular";
            case GameAction.Fire: return "Travar cursor";
            case GameAction.AttackLine1: return "Ataque 1";
            case GameAction.AttackLine2: return "Ataque 2";
            case GameAction.AttackLine3: return "Ataque 3";
            case GameAction.AttackLine4: return "Ataque 4";
            case GameAction.RangedFire: return "Disparo";
            case GameAction.Interact: return "Interagir";
            case GameAction.Dash: return "Dash";
            case GameAction.Pause: return "Pausar";
            case GameAction.Submit: return "Confirmar";
            case GameAction.Cancel: return "Cancelar";
            case GameAction.NavigateUp: return "Navegar acima";
            case GameAction.NavigateDown: return "Navegar abaixo";
            case GameAction.NavigateLeft: return "Navegar esquerda";
            case GameAction.NavigateRight: return "Navegar direita";
            default: return action.ToString();
        }
    }

    public static List<KeybindingEntry> CreateDefaultKeybindings()
    {
        var entries = new List<KeybindingEntry>();
        foreach (GameAction action in RebindableActions)
        {
            entries.Add(new KeybindingEntry
            {
                action = action,
                keyCode = GetDefaultKey(action).ToString()
            });
        }

        return entries;
    }
}
