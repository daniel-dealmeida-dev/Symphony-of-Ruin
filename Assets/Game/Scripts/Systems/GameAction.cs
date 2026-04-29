using System;

public enum GameAction
{
    MoveLeft,
    MoveRight,
    Jump,
    Fire,
    Interact,
    Dash,
    Pause,
    Submit,
    Cancel,
    NavigateUp,
    NavigateDown,
    NavigateLeft,
    NavigateRight,
    RangedFire
}

[Serializable]
public class KeybindingEntry
{
    public GameAction action;
    public string keyCode;
}
