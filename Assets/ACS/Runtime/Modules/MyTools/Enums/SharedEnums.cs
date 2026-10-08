public static class SharedEnums
{
    [System.Serializable]
    public enum DefaultEvents
    {
        None,
        OnEnable,
        OnDisable,
        OnDestroy,
    }
    

    [System.Flags, System.Serializable]
    public enum DefaultEvents_new
    {
        None    =    0,
        OnEnable    =    1    << 0,
        OnDisable    =    1    << 1,
        OnDestroy    =    1    << 2,
    }

}




