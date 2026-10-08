using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour 
{ 
    [SerializeField] private PoolEnqueueType PoolEnqueueType = PoolEnqueueType.None;
    [SerializeField] private string SingleKey = "Key"; 
    [SerializeField] private bool ShuffleQueues = false;

    public string singleKey => SingleKey;

    public List<Pool> pools; // List of pools
    public Dictionary<string, Queue<GameObject>> poolDictionary; // Dictionary for quick access to pools

    //public IEnumerator SetPoolCount(LevelBase levelbase)
    //{
    //    foreach (var Counter in levelbase.NPCCounterList)
    //    {
    //        int poolID = pools.FindIndex(entry => entry.ColorSelection == Counter.colorType);
    //        //Pool pool = pools.Find(pools => pools.ColorSelection == Counter.colorType);
    //        Debug.Log($"Pool ID: {poolID}, Counter {Counter.colorType}, Size: {Counter.NPCCounter}");
    //        if (poolID >= 0)
    //            pools[poolID].size = Counter.NPCCounter;
    //        yield return new WaitForEndOfFrame();
    //    }
    //}

    public IEnumerator CreatePool()
    {
        if (PoolEnqueueType == PoolEnqueueType.None)
            yield break;

        if (PoolEnqueueType == PoolEnqueueType.Separate)
            yield return CreatePool_Separate();
        else if (PoolEnqueueType == PoolEnqueueType.Single)
        {
            yield return CreatePool_Single();
            if(ShuffleQueues) ShuffleQueue(SingleKey);
        }

    }

    IEnumerator CreatePool_Single()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        Queue<GameObject> objectPool = new();

        // Initialize the pools
        foreach (Pool pool in pools)
        {
            // Create and store pooled objects
            if(pool.size > 0)
                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab);
                    obj.name = pool.ColorSelection.ToString() + Random.Range(1000, 5001).ToString();
                    //obj.GetComponent<NPCController>().SetColor(pool.ColorSelection);  // Additional Code
                    obj.SetActive(false); // Disable the object initially
                    objectPool.Enqueue(obj); // Add to the queue
                }
            yield return new WaitForEndOfFrame();
        }
        poolDictionary.Add(SingleKey, objectPool); // Add the pool to the dictionary
        Debug.Log("Dictionary Length: " + poolDictionary.Count);
        yield return null;
    }

    IEnumerator CreatePool_Separate()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // Initialize the pools
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new();

            // Create and store pooled objects
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false); // Disable the object initially
                objectPool.Enqueue(obj); // Add to the queue
                yield return new WaitForEndOfFrame();
            }
            poolDictionary.Add(pool.tag, objectPool); // Add the pool to the dictionary
        }
        Debug.Log("Dictionary Length: " + poolDictionary.Count);
        yield return null;
    }

    public void ShuffleQueue(string Key)
    {
        poolDictionary[Key].Shuffle();
    }


    public int GetSinglePoolSize()
    {
        if (poolDictionary.ContainsKey(SingleKey))
        {
            int poolSize = poolDictionary[singleKey].Count;
            //Debug.Log($"Pool '{SingleKey}' has {poolSize} objects.");
            return poolSize;
        }
        else
        {
            Debug.LogWarning($"Pool with tag '{SingleKey}' does not exist.");
            return 0;
        }
    }
    
    public int GetPoolSize(string poolTag)
    {
        if (poolDictionary.ContainsKey(poolTag))
        {
            int poolSize = poolDictionary[poolTag].Count;
            //Debug.Log($"Pool '{poolTag}' has {poolSize} objects.");
            return poolSize;
        }
        else
        {
            Debug.LogWarning($"Pool with tag '{poolTag}' does not exist.");
            return 0;
        }
    }



    // Method to get an object from the pool
    public GameObject SpawnFromPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue(); // Get the object from the pool
     
        return objectToSpawn; // Return the spawned object
    }  
    
    public GameObject SpawnFromSinglePool(bool IsActive)
    {
        if (!poolDictionary.ContainsKey(singleKey))
        {
            Debug.LogWarning($"Pool with tag {singleKey} doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[singleKey].Dequeue(); // Get the object from the pool
        objectToSpawn.SetActive(IsActive); // Activate the object
      
        return objectToSpawn; // Return the spawned object
    }

    public GameObject SpawnFromPool(string tag, bool IsActive)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue(); // Get the object from the pool
        objectToSpawn.SetActive(IsActive); // Activate the object
      
        return objectToSpawn; // Return the spawned object
    }
    
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue(); // Get the object from the pool
        objectToSpawn.SetActive(true); // Activate the object
        objectToSpawn.transform.position = position; // Set position
        objectToSpawn.transform.rotation = rotation; // Set rotation

        return objectToSpawn; // Return the spawned object
    }
    public void EnqueueObject(string tag, GameObject objectToReturn)
    {
        objectToReturn.SetActive(false); // Deactivate the object before returning to pool
        poolDictionary[tag].Enqueue(objectToReturn); // Add it back to the pool
    }
}


[System.Serializable]
public class Pool
{
    public string tag = "Untagged"; // Identifier for the pool
    public ColorType ColorSelection = ColorType.None;
    public GameObject prefab = null; // Prefab to pool
    public int size = 1; // Number of objects to pool
}

[System.Serializable]
public  enum PoolEnqueueType
{
    None, Single, Separate
}

public static class QueueExtensions
{
    // Extension method to shuffle the queue
    public static void Shuffle<T>(this Queue<T> queue)
    {
        // Convert the queue to a list
        List<T> list = new List<T>(queue);

        // Shuffle the list using Fisher-Yates algorithm
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1); // Get a random index
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }

        // Clear the original queue
        queue.Clear();

        // Enqueue the shuffled items back into the queue
        foreach (T item in list)
        {
            queue.Enqueue(item);
        }
    }
}




