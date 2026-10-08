using ACS;
using System;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class InternetConnectivity
{
    public InternetConnectivity()
    {
      
    }
     
    internal bool checkInternet = true;
    internal Action OnInternetLost;
    internal Action OnInternetRestored;

    enum Status
    {
        None, 
        Shown, 
        Hidden
    }

    private Status status = Status.Hidden;
    
    internal IEnumerator CheckInternet()
    {
        if(!checkInternet)
        {
            Debug.LogError("CheckInternet is set to false. Internet connectivity check is disabled.");
            yield break;
        }

        while (true)
        {
            //Debug.Log("Intnet Connectivity: " + (Application.internetReachability == NetworkReachability.NotReachable));
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (status == Status.Hidden)
                {
                    UIManager.Instance.ShowPanel_Ind(PanelType.LoadingPopup);
                    status = Status.Shown;
                }
            }
            else
            {
                if (status == Status.Shown)
                {
                    UIManager.Instance.HidePanel_Ind();
                    status = Status.Hidden;
                }
            }

            Time.timeScale = Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1;
            yield return new WaitForSecondsRealtime(1);
        }
         
    }
}






