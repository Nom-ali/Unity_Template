using UnityEngine;

public class EventPublisher : MonoBehaviour
{
    public enum PublisherType
    {
        OnEnable,
        OnDisable,
        OnAwake,
        OnStart,
        OnDestroy,
    }

    [System.Serializable]
    public struct PublishEvent
    {
        public PublisherType publisherType;
        public EventKeys eventKey;
    }

    [SerializeField] private PublishEvent[] eventsToPublish;

    void m_PublishEvent(PublisherType type)
    {
        if(eventsToPublish == null || eventsToPublish.Length == 0)
        {
            return;
        }

        for (int i = 0; i < eventsToPublish.Length; i++)
        {
            if (type == eventsToPublish[i].publisherType)
            {
                GameEvents.Publish(eventsToPublish[i].eventKey);
            }
        }
    }

    private void OnEnable()
    {
        m_PublishEvent(PublisherType.OnEnable);
    }

    private void OnDisable()
    {
        m_PublishEvent(PublisherType.OnDisable);
    }

    private void Awake()
    {
        m_PublishEvent(PublisherType.OnAwake);
    }

    private void Start()
    {
        m_PublishEvent(PublisherType.OnStart);
    }

    private void OnDestroy()
    {
        m_PublishEvent(PublisherType.OnDestroy);
    }
}
