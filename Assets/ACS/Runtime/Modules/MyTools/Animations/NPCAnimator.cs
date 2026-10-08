using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Range(0, 10)] private int rangeValue;
    [SerializeField] private bool PlayInLoop = false;
   
    private void Start()
    {
        animator ??= GetComponent<Animator>();

    }

    public void PlayRandAnimation()
    {
        StartCoroutine(PlayRandomAnimation());
    }

    public IEnumerator PlayRandomAnimation()
    {
        if (!PlayInLoop)
        {
            int value = Random.Range(1, rangeValue);
            PlayAnimation(value);
        }
        else
        {
            while (true)
            {
                int value = Random.Range(1, rangeValue);
                PlayAnimation(value);
                float duration = animator.GetCurrentAnimatorStateInfo(0).length;
                yield return new WaitForSeconds(duration);
            }
        }
    }

    public void ResetAnimator()
    {
        animator.SetBool("NextClicked", true);
    }

    public void PlayAnimation(int index)
    {
        animator.SetBool("NextClicked", false);
        animator.Play("Talking_"+index);
    }
}




