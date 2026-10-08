#if ACS_DOTWEEN
using DG.Tweening;
using System.Collections;
using UnityEngine;

public class Logo : MonoBehaviour
{
    [SerializeField] private GameObject[] logos;

    private void OnEnable()
    {
        StartCoroutine(StartAnimation());
    }

    IEnumerator StartAnimation()
    {
        while (true)
        {
            for (int i = 0; i < logos.Length; i++)
            {
                // Capture the current index in a local variable
                int index = i;
                float y_Pos = logos[index].transform.localPosition.y;

                // Move the logo up
                logos[index].transform.DOLocalMoveY(0.1f, 0.5f).OnComplete(() =>
                {
                    // Move it back to its original position
                    logos[index].transform.DOLocalMoveY(y_Pos, 0.5f);
                });

                // Wait for a short period before moving the next logo
                yield return new WaitForSeconds(0.2f);
            }

            // Wait for 2 seconds before starting the loop again
            yield return new WaitForSeconds(2);
        }
    }

}
#endif

