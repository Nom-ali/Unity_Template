using ACS;
using UnityEngine;

public interface IInit
{
    public void Init();
}

public interface IInitialization
{
    public void Init(UIManagerBase uIManager);
}

public interface IInitialization_Level
{
    public void Init(GameObject player);
}


