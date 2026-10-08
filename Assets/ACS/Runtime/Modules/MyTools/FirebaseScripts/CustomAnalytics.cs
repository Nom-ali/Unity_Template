#if ACS_FIREBASE
using Firebase;
using Firebase.Analytics;
using System.Text;
using UnityEngine;

public class CustomAnalytics : MonoBehaviour
{
    public bool isFireBaseOkToUse = false;
    char[] specialChars = new char[] { '.', ',', '!', '?', '@', '#', '$', '%', '^', '&', '*', ':', ';', '"', '<', '>', '|', '[', ']', '(', ')', '-' };

    internal void LogEvent(params string[] messages)
    {
        string combinedEvent = string.Join("_", messages);
        m_LogEvent(combinedEvent);
    }

    internal void LogEvent(string messages, Parameter[] parameters)
    {
        string combinedEvent = string.Join("_", messages);
        m_LogEvent(combinedEvent, parameters);
    }

    string ReplaceSpecialCharacters(string input)
    {
        char[] inputArray = input.ToCharArray();

        for (int i = 0; i < inputArray.Length; i++)
        {
            if (System.Array.Exists(specialChars, element => element == inputArray[i]) || inputArray[i] == ' ')
            {
                inputArray[i] = '_';
            }
        }

        return new string(inputArray);
    }

    internal void Initialize(DependencyStatus dependencyStatus)
    {
        if (dependencyStatus == DependencyStatus.Available)
        {
            isFireBaseOkToUse = true;
            m_LogEvent("Game Started");
        }
        else
        {
            Debug.LogError(string.Format("Analytics: Dependencies resolve error: {0}", dependencyStatus));
        }
    }

    private void m_LogEvent(string eventNames)
    {
        if (!isFireBaseOkToUse || string.IsNullOrEmpty(eventNames))
        {
            Debug.Log("Analytics: Message is empty or Firebase is not Ok To Use ");
            return;
        }

        string newString = ReplaceSpecialCharacters(eventNames);

        try
        {
            FirebaseAnalytics.LogEvent(newString);
            Debug.Log("Analytics: " + newString);
        }
        catch (System.Exception e)
        {
            Debug.Log("Analytics: Error in Analytics: " + e.ToString());
        }
    }

    private void m_LogEvent(string eventName, Parameter[] parameters)
    {
        if (!isFireBaseOkToUse || string.IsNullOrEmpty(eventName))
        {
            Debug.Log("Analytics: Message is empty or Firebase is not Ok To Use ");
            return;
        }

        string newString = ReplaceSpecialCharacters(eventName);

        try
        {
            FirebaseAnalytics.LogEvent(newString, parameters);
            
            Debug.Log("Analytics: " + newString + " | " + createParameterString(parameters));
        }
        catch (System.Exception e)
        {
            Debug.Log("Analytics: Error in Analytics: " + e.ToString());
        }
    }


    string createParameterString(Parameter[] parameters)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < parameters.Length; i++)
        {
            sb.Append(parameters[i].ToString());
            sb.Append(" | ");
        }
        return sb.ToString();
    }
}
#else
using UnityEngine;

public class CustomAnalytics : MonoBehaviour
{
}
#endif




