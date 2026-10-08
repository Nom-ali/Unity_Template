using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LifeSystem : MonoBehaviour
{
    public static Action<Action, Action> Died;
    public static Action Revived;

    [SerializeField] private int NumberOfLives; // Total lives the player starts with
    [SerializeField] private RectTransform Container; // UI container for life icons
    [SerializeField] private Image Prefab; // Prefab of the life icon
    [SerializeField] private Sprite AliveSprite; // Sprite when the life is alive
    [SerializeField] private Sprite DeadSprite; // Sprite when the life is dead

    [SerializeField] private List<Image> Lives = new List<Image>(); // List to store life images

    private void OnEnable()
    {
        Died += DiedOnce;
        Revived += Revive;
    }

    private void OnDisable()
    {
        Died -= DiedOnce;
        Revived -= Revive;
    }

    private void Start()
    {
        CreateLives(); // Initialize the life icons at the start
    }

    // Create lives UI icons
    void CreateLives()
    {
        // Create the number of lives icons based on the NumberOfLives
        for (int i = 0; i < NumberOfLives; i++)
        {
            Image tempVar = Instantiate(Prefab, Container);
            Lives.Add(tempVar);
            tempVar.sprite = AliveSprite; // Set initial sprite to Alive
            tempVar.gameObject.SetActive(true); // Ensure all icons are visible at the start
        }
    }

    // Call this method when the player dies
    void DiedOnce(Action onDead, Action onLevelFailed = null)
    {

        // Find the first life that is currently alive
        for (int i = Lives.Count - 1; i >= 0; i--)
        {
            if (Lives[i].sprite == AliveSprite)
            {
                // Set the sprite of the first "alive" life as Dead
                Lives[i].sprite = DeadSprite;
                return; // Exit the method after marking the first life as dead
            }
        }

        if(Lives.TrueForAll(life => life.sprite == DeadSprite))
        {
            onLevelFailed?.Invoke();
            Debug.Log("********** <color=red> LEVEL FAILED </color> **********");
            return;
        }

        // If there are no alive lives left, we can add game over logic here
        Debug.Log("Game Over!");
    }

    // Call this method to revive the player
    void Revive()
    {
        // Find the first dead life and set it back to alive
        for (int i = 0; i < Lives.Count; i++)
        {
            if (Lives[i].sprite == DeadSprite)
            {
                // Set the sprite of the first "dead" life as Alive
                Lives[i].sprite = AliveSprite;
                return; // Exit the method after reviving the first dead life
            }
        }

        // If there are no dead lives left (and lives are less than the max), create a new one
        //if (Lives.Count < NumberOfLives)
        //{
        //    Image newLife = Instantiate(Prefab, Container);
        //    newLife.sprite = AliveSprite;
        //    Lives.Add(newLife); // Add the new life to the list
        //}
    }
}




