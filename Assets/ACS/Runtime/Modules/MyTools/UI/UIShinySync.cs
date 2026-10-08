//using Coffee.UIEffects;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//public class UIShinySync : MonoBehaviour
//{

//    [SerializeField] private float Durattion;
//    [SerializeField] private float Delay;
//    [SerializeField] private UIShiny[] UIShinies;

//    private void Start()
//    {
//        StartCoroutine(ShinySync());
//    }

//    private IEnumerator ShinySync()
//    {
//        while (enabled)
//        {
//            for (int i = 0; i < UIShinies.Length; i++)
//            {
//                UIShinies[i].effectPlayer.duration = Durattion;
//                UIShinies[i].Play();
//                yield return new WaitForSeconds(Delay);
//            }
//            yield return null;
//        }
//    }

   
//}




