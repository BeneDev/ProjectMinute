﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpFloating : MonoBehaviour {

    public bool Equipped
    {
        get
        {
            return equipped;
        }
        set
        {
            equipped = value;
        }
    }

    [SerializeField] protected Vector3 movement = new Vector3(10f, 0, 0);
    [Range(0, 1)] protected float movementFactor;
    [Range(0, 20), SerializeField] protected float period = 2f;

    protected SpriteRenderer rend;

    protected bool equipped = false;

    protected Vector3 startPos;

    // TODO Awake gets not called properly
    protected virtual void Awake()
    {
        startPos = transform.position; // gets the starting position of the object
        rend = GetComponent<SpriteRenderer>();
    }
    
    protected virtual void Update()
    {
        if(!equipped)
        {
            //prevent dividing by 0
            if (period <= Mathf.Epsilon) // Epsilon is the tiniest value a float can have so dont compare floats to zero (too inconsistent), but compare them to epsilon and never as "==" rather than "<="
            {
                return;
            }

            float cycles = Time.time / period; // determines how far into the sin wave we are

            const float tau = Mathf.PI * 2; // about 6.28
            float rawSinWave = Mathf.Sin(cycles * tau); // goes from -1 to +1

            movementFactor = rawSinWave / 2f + 0.5f; // goes from 0 to +1

            transform.position = startPos + (movement * movementFactor); // adds the current movement to the object's transform
        }
        else if (rend.enabled)
        {
            rend.enabled = false;
        }
    }

}
