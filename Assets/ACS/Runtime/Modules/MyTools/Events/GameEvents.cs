using System;
using System.Collections.Generic;

/// <summary>
/// A static event management system that provides a centralized way to subscribe, unsubscribe, and publish events.
/// Uses a dictionary-based approach to manage event delegates dynamically.
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// The central event table that stores all registered event delegates, keyed by event name.
    /// </summary>
    private static readonly Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();

    /// <summary>
    /// Gets or sets a value indicating whether missing events should be logged as warnings.
    /// </summary>
    public static bool LogMissingEvents = true;

    /// <summary>
    /// Resets the event system when entering Play Mode in the Unity Editor.
    /// This ensures a clean slate for each play session.
    /// </summary>
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayMode()
    {
        Clear();
    }


    #region Subscription Methods
    /// <summary>
    /// Subscribes a listener to an event identified by an enum key.
    /// </summary>
    /// <param name="key">The enum key identifying the event.</param>
    /// <param name="listener">The action to invoke when the event is published.</param>
    public static void Subscribe(Enum key, Action listener)
    {
        Subscribe(ToKey(key), listener);
    }

    /// <summary>
    /// Subscribes a listener to an event identified by an enum key, passing a generic parameter.
    /// </summary>
    /// <typeparam name="T">The type of data passed to the listener.</typeparam>
    /// <param name="key">The enum key identifying the event.</param>
    /// <param name="listener">The action to invoke when the event is published.</param>
    public static void Subscribe<T>(Enum key, Action<T> listener)
    {
        Subscribe(ToKey(key), listener);
    }

    /// <summary>
    /// Subscribes a listener to an event identified by a string key.
    /// </summary>
    /// <param name="key">The string key identifying the event.</param>
    /// <param name="listener">The action to invoke when the event is published.</param>
    /// <exception cref="ArgumentNullException">Thrown when listener is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the event key is already registered with a different listener type.</exception>
    public static void Subscribe(string key, Action listener)
    {
        key = ValidateKey(key);

        if (listener == null)
            throw new ArgumentNullException(nameof(listener));

        if (eventTable.TryGetValue(key, out var existingDelegate))
        {
            if (!(existingDelegate is Action))
                throw new InvalidOperationException($"Event key '{key}' is already registered with a different listener type.");

            eventTable[key] = Delegate.Combine(existingDelegate, listener);
        }
        else
        {
            eventTable.Add(key, listener);
        }
    }

    /// <summary>
    /// Subscribes a listener to an event identified by a string key, passing a generic parameter.
    /// </summary>
    /// <typeparam name="T">The type of data passed to the listener.</typeparam>
    /// <param name="key">The string key identifying the event.</param>
    /// <param name="listener">The action to invoke when the event is published.</param>
    /// <exception cref="ArgumentNullException">Thrown when listener is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the event key is already registered with a different listener type.</exception>
    public static void Subscribe<T>(string key, Action<T> listener)
    {
        key = ValidateKey(key);

        if (listener == null)
            throw new ArgumentNullException(nameof(listener));

        if (eventTable.TryGetValue(key, out var existingDelegate))
        {
            if (!(existingDelegate is Action<T>))
                throw new InvalidOperationException($"Event key '{key}' is already registered with a different listener type.");

            eventTable[key] = Delegate.Combine(existingDelegate, listener);
        }
        else
        {
            eventTable.Add(key, listener);
        }
    }
    #endregion Subscription Methods

    #region Unsubscription Methods
    /// <summary>
    /// Unsubscribes a listener from an event identified by an enum key.
    /// </summary>
    /// <param name="key">The enum key identifying the event.</param>
    /// <param name="listener">The action to remove from the event.</param>
    public static void Unsubscribe(Enum key, Action listener)
    {
        Unsubscribe(ToKey(key), listener);
    }

    /// <summary>
    /// Unsubscribes a listener from an event identified by an enum key, handling a generic parameter.
    /// </summary>
    /// <typeparam name="T">The type of data passed to the listener.</typeparam>
    /// <param name="key">The enum key identifying the event.</param>
    /// <param name="listener">The action to remove from the event.</param>
    public static void Unsubscribe<T>(Enum key, Action<T> listener)
    {
        Unsubscribe(ToKey(key), listener);
    }

    /// <summary>
    /// Unsubscribes a listener from an event identified by a string key.
    /// </summary>
    /// <param name="key">The string key identifying the event.</param>
    /// <param name="listener">The action to remove from the event.</param>
    public static void Unsubscribe(string key, Action listener)
    {
        key = ValidateKey(key);

        if (listener == null)
            return;

        if (eventTable.TryGetValue(key, out var existingDelegate) && existingDelegate is Action)
        {
            var currentDelegate = Delegate.Remove(existingDelegate, listener);

            if (currentDelegate == null)
                eventTable.Remove(key);
            else
                eventTable[key] = currentDelegate;
        }
    }

    /// <summary>
    /// Unsubscribes a listener from an event identified by a string key, handling a generic parameter.
    /// </summary>
    /// <typeparam name="T">The type of data passed to the listener.</typeparam>
    /// <param name="key">The string key identifying the event.</param>
    /// <param name="listener">The action to remove from the event.</param>
    public static void Unsubscribe<T>(string key, Action<T> listener)
    {
        key = ValidateKey(key);

        if (listener == null)
            return;

        if (eventTable.TryGetValue(key, out var existingDelegate) && existingDelegate is Action<T>)
        {
            var currentDelegate = Delegate.Remove(existingDelegate, listener);

            if (currentDelegate == null)
                eventTable.Remove(key);
            else
                eventTable[key] = currentDelegate;
        }
    }

    /// <summary>
    /// Removes all listeners from an event identified by an enum key.
    /// </summary>
    /// <param name="key">The enum key identifying the event.</param>
    public static void UnsubscribeAll(Enum key)
    {
        UnsubscribeAll(ToKey(key));
    }

    /// <summary>
    /// Removes all listeners from an event identified by a string key.
    /// </summary>
    /// <param name="key">The string key identifying the event.</param>
    public static void UnsubscribeAll(string key)
    {
        key = ValidateKey(key);
        eventTable.Remove(key);
    }

    /// <summary>
    /// Clears all events and listeners from the event table.
    /// </summary>
    public static void Clear()
    {
        eventTable.Clear();
    }
    #endregion Unsubscription Methods


    #region Publish Methods
    /// <summary>
    /// Publishes an event identified by an enum key, invoking all subscribed listeners.
    /// </summary>
    /// <param name="key">The enum key identifying the event.</param>
    public static void Publish(Enum key)
    {
        Publish(ToKey(key));
    }

    /// <summary>
    /// Publishes an event identified by an enum key with generic event data, invoking all subscribed listeners.
    /// </summary>
    /// <typeparam name="T">The type of data to pass to the listeners.</typeparam>
    /// <param name="key">The enum key identifying the event.</param>
    /// <param name="eventData">The data to pass to each listener.</param>
    public static void Publish<T>(Enum key, T eventData)
    {
        Publish(ToKey(key), eventData);
    }

    /// <summary>
    /// Publishes an event identified by a string key, invoking all subscribed listeners.
    /// </summary>
    /// <param name="key">The string key identifying the event.</param>
    public static void Publish(string key)
    {
        key = ValidateKey(key);

        if (!eventTable.TryGetValue(key, out var existingDelegate))
        {
            LogMissingEvent(key);
            return;
        }

        var callback = existingDelegate as Action;

        if (callback == null)
        {
            LogWrongEventType(key);
            return;
        }

        callback.Invoke();
    }

    /// <summary>
    /// Publishes an event identified by a string key with generic event data, invoking all subscribed listeners.
    /// </summary>
    /// <typeparam name="T">The type of data to pass to the listeners.</typeparam>
    /// <param name="key">The string key identifying the event.</param>
    /// <param name="eventData">The data to pass to each listener.</param>
    public static void Publish<T>(string key, T eventData)
    {
        key = ValidateKey(key);

        if (!eventTable.TryGetValue(key, out var existingDelegate))
        {
            LogMissingEvent(key);
            return;
        }

        var callback = existingDelegate as Action<T>;

        if (callback == null)
        {
            LogWrongEventType(key);
            return;
        }

        callback.Invoke(eventData);
    }
    #endregion Publish Methods

    #region Utility Methods
    /// <summary>
    /// Converts an enum key to its string representation.
    /// </summary>
    /// <param name="key">The enum key to convert.</param>
    /// <returns>The string representation of the enum key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    private static string ToKey(Enum key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        return key.ToString();

        // Safer alternative:
        // return key.GetType().FullName + "." + key;
    }

    /// <summary>
    /// Validates that an event key is not null, empty, or whitespace.
    /// </summary>
    /// <param name="key">The string key to validate.</param>
    /// <returns>The validated key.</returns>
    /// <exception cref="ArgumentException">Thrown when key is null, empty, or whitespace.</exception>
    private static string ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Event key cannot be null, empty, or whitespace.", nameof(key));

        return key;
    }

    /// <summary>
    /// Logs a warning when attempting to publish an event that has no subscribers.
    /// </summary>
    /// <param name="key">The event key that was not found.</param>
    private static void LogMissingEvent(string key)
    {
        if (!LogMissingEvents)
            return;

        UnityEngine.Debug.LogWarning("<color=yellow>No event found against key:</color> " + key);
    }

    /// <summary>
    /// Logs an error when the listener type does not match the publish type.
    /// </summary>
    /// <param name="key">The event key with the type mismatch.</param>
    private static void LogWrongEventType(string key)
    {
        UnityEngine.Debug.LogError("<color=red>Event found but listener type does not match publish type. Key:</color> " + key);
    }
    #endregion Utility Methods
}