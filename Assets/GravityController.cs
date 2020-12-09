using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityController : MonoBehaviour
{
    public Transform room;
    // Update is called once per frame
    void Update()
    {
        
    }

    private void SetGravityDefault()
    {
        transform.rotation = Quaternion.identity;
        room.position = new Vector3(0, 1.5f, 0);
    }

    private void SetGravityUpsideDown()
    {
        room.rotation = Quaternion.Euler(new Vector3(180, 0, 0));
        room.position = new Vector3(3.5f, 0, 0);
    }
}
