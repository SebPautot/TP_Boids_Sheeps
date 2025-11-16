using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepHerd : MonoBehaviour
{

    [Tooltip("List of all the sheeps in the herd")]
    [HideInInspector]
    public SheepBoid[] sheeps;

    public static List<SheepHerd> Instances = new List<SheepHerd>();

    void Awake()
    {
        Instances.Add(this);
    }

    void Start()
    {
        //Find all sheeps in children
        sheeps = GetComponentsInChildren<SheepBoid>(false);

        foreach (SheepBoid sheepBoid in sheeps)
        {
            sheepBoid.herd = this;
        }
    }

    public Vector3 averageHerd = Vector3.zero;
    public Vector3 predator = Vector3.zero;
    void FixedUpdate()
    {
        if (sheeps.Length < 1)
            return;

        predator = Vector3.zero;
        foreach (SheepHerd sheepHerd in SheepHerd.Instances)
        {
            if (sheepHerd == this)
                continue;
            
            predator += sheepHerd.averageHerd;
        }
        predator /= SheepHerd.Instances.Count;

        averageHerd = Vector3.zero;
        foreach (SheepBoid sheepBoid in sheeps)
        {
            averageHerd += sheepBoid.transform.position;
        }
        averageHerd /= sheeps.Length;

        
    }
}
