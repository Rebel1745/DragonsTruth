using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float Speed = 5f;
    public Rigidbody2D rb;
    public float TimeToDestroy = 5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        rb.velocity = transform.right * Speed;

        TimeToDestroy -= Time.deltaTime;

        if(TimeToDestroy <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
