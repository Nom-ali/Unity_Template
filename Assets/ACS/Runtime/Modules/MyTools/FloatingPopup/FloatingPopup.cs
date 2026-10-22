using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FloatingPopup : MonoBehaviour
{
    [SerializeField] private Text Message;
    [SerializeField] private Image image;
    [SerializeField] private float MovingSpeed = 50;

    private Graphic[] graphics;

   
    public void SetupMessage(string messgae)
    {
        StartCoroutine(SetMessage(messgae, 5));
    }                                      

    public void SetupMessage(string messgae, float duration)
    {
        StartCoroutine(SetMessage(messgae, duration));
    }

    private IEnumerator SetMessage(string message, float duration)
    {
        if(Message == null)
            Message = GetComponentInChildren<Text>(true);

        if (image == null)
            image = GetComponent<Image>();

        if(graphics == null)
            graphics = GetComponentsInChildren<Graphic>(true);

        yield return new WaitUntil(() => message != null&& image != null);

        Message.text = message;

        if (Message.gameObject.activeInHierarchy == false)
            Message.gameObject.SetActive(true);

        if(image.enabled == false)
            image.enabled = true;
        
        yield return Run(duration);
    }

    private IEnumerator Run(float duration)
    {
        float time = duration;

        Invoke(nameof(Fade), time / 4);
        
        while (time > 0)
        {
            transform.Translate(MovingSpeed * Time.deltaTime * Vector3.up);
            time -= Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    void Fade()
    {
        foreach (Graphic graphic in graphics)
            graphic.CrossFadeAlpha(0, 1, true);
    }

}




