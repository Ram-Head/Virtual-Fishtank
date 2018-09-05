using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class swimtest : MonoBehaviour
{

    private float inputH;
    private float inputV;
    public Animator anim;
    public Rigidbody rbody;
    private float speed = 200f;
    private float rotSpeed = 90f;
    bool automateTurnAnimation = true;
    private float desiredInputH = 0;
    private float desiredInputV = 0;


    void Start()
    {
        rbody = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        StartCoroutine("automateTurnAnim");
    }

    void Update()
    {
        if (avoidCoroutine == null)
        {
            float moveX = inputH * 80f * Time.deltaTime;
            float moveY = -inputV * 80f * Time.deltaTime;
            rotateBody(rbody, new Vector3(moveY, moveX, 0));
            rbody.velocity = transform.forward * speed * Time.deltaTime;
        }

    }

    Coroutine avoidCoroutine = null;
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("ENTER");
        if (collision.gameObject.tag == "Wall")
        {
            if (avoidCoroutine == null)
            {
                avoidCoroutine = StartCoroutine("avoidWall", collision);
            }

        }
        else if (collision.gameObject.tag == "Fish")
        {
            //StartCoroutine("avoidFish");
        }

    }

    /*
     * Fish avoids wall by
     */
    private IEnumerator avoidWall(Collision collision)
    {
        Debug.Log("Avoid");

        //Adds all the collision points for collision up so they can then 
        Vector3 contact = Vector3.zero;
        for (int i = 0; i < collision.contacts.Length; i++)
        {
            Debug.Log("Collision point" + "i" + ": " + collision.contacts[i].normal);
            contact += collision.contacts[i].normal;
        }
        Quaternion currRot = rbody.transform.rotation;
        Vector3 facingVector = currRot * Vector3.forward;
        Vector3 collisionNormal = (Vector3.Normalize(contact)) * -1;
        Vector3 newVector = Vector3.Reflect(facingVector, collisionNormal);
        float angle = Vector3.SignedAngle(transform.forward, newVector, Vector3.up);
        float sign = Mathf.Sign(angle);
        Debug.Log(angle);

        //Rotates fish to new angle at whatever speed rotSpeed is set to
        float currAngle = 0;
        desiredInputH = sign * 1;
        // StartCoroutine("automateTurnAnim", sign * 1);
        while (currAngle * sign < angle * sign)
        {
            //Debug.Log(currAngle);
            float dAngle = sign * rotSpeed * Time.deltaTime;
            rotateBody(rbody, new Vector3(0, dAngle, 0));
            currAngle += dAngle;
            yield return null;
        }
        //StartCoroutine("automateTurnAnim",0);
        desiredInputH = 0;
        avoidCoroutine = null;
        Debug.Log("Done!");
    }

    private void slightTurn(ref float currVal, ref float goalVal, string animName, float speedMultiplier)
    {
        Debug.Log("CURRR: " + currVal);
        float sign = Mathf.Sign(goalVal - currVal);
        Debug.Log(animName + ": " + goalVal + ", Dif: " + rotSpeed * speedMultiplier);
        currVal += sign * rotSpeed * speedMultiplier;
        if (currVal * sign > goalVal * sign) { currVal = goalVal; }
        anim.SetFloat(animName, currVal);
    }

    /*
     * Ensures that when the fish turns, its turn animation plays smoothly by 
     */
    private IEnumerator automateTurnAnim()
    {
        while (automateTurnAnimation)
        {
            //Debug.Log("check turn");
            if (inputH != desiredInputH)
            {
                Debug.Log("turning");
                //slightTurn(ref inputH, ref desiredInputH, "inputH", 0.0005f);
                float sign = Mathf.Sign(inputH - desiredInputH);
                //Debug.Log(animName + ": " + goalVal + ", Dif: " + rotSpeed * speedMultiplier);
                inputH += sign * rotSpeed * 0.00005f;
                if (inputH * sign > desiredInputH * sign) { inputH = desiredInputH; }
                anim.SetFloat("inputH", inputH);
            }

            if (inputV != desiredInputV)
            {
                Debug.Log("turning");
                slightTurn(ref inputV, ref desiredInputV, "inputV", 0.0005f);
            }
            yield return null;
        }

    }

    //speed, rotspeed, arc, turn angle x, turn angle y
    private void randomizeMovement()
    {
        //float randy = Random.Range(0, 2);
    }

    private void rotateBody(Rigidbody rbody, Vector3 angleVel)
    {
        Quaternion deltaRotation = Quaternion.Euler(angleVel);
        rbody.MoveRotation(rbody.rotation * deltaRotation);
    }
}
