using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    public Camera mainCamera;
    public Ball ball;

    public Vector3 hover;
    private bool holding;
    public float holdingTime;

    public float maxBumpSpeed = 10;

    void Update()
    {
        var mousePosition = Input.mousePosition;
        if (Input.GetButtonDown("Fire3"))
        {
            mousePosition.z = mainCamera.transform.position.y;
            ball.transform.position = mainCamera.ScreenToWorldPoint(mousePosition);
            ball.CheatJumpTo(mainCamera.ScreenToWorldPoint(mousePosition));
        }
        else if (Input.GetButtonDown("Fire1"))
        {
            mousePosition.z = mainCamera.transform.position.y;
            holding = true;
            holdingTime = 0;
            ball.PrepareBump();
        }
        else if (Input.GetButtonUp("Fire1") && holding)
        {
            mousePosition.z = mainCamera.transform.position.y;
            hover = mainCamera.ScreenToWorldPoint(mousePosition);
            holding = false;
            ball.Bump(-Ball.BumpSpeed(ball.transform.position, ball.velocity, maxBumpSpeed, hover, holdingTime));
        }
        else if (holding)
        {
            holdingTime += Time.deltaTime;
            mousePosition.z = mainCamera.transform.position.y;
            hover = mainCamera.ScreenToWorldPoint(mousePosition);
            hover.y = 0;
        }

        if (Input.GetButtonDown("Fire2"))
        {
            if (Input.GetButton("Fire1")) // Abort with R-Click
            {
                holding = false;
                if (ball.velocity.sqrMagnitude != 0)
                {
                    ball.UnFreeze();
                }
            }
            else
            {
                ball.EngangeBreaks();
            }
        }


        if (Input.GetButtonDown("BackSpace"))
        {
            ball.dead = true;
            ball.PrepareBump();
            ball.UnFreeze();
        }

        if (holding)
        {
            ball.GenerateTrajectory(maxBumpSpeed, hover, holdingTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (!holding)
        {
            return;
        }

        var trajectory = ball.trajectory;
        Debug.Log(trajectory.Length);
        for (int i = 0; i < trajectory.Length; i++)
        {
            if (i == trajectory.Length - 1 && i != trajectory.Capacity - 1)
            {
                Handles.color = Color.red;
            }
            else
            {
                Handles.color = Color.white;
            }
            
            Handles.DrawWireDisc(trajectory[i].Item1, Vector3.up, ball.radius);
        }
    }
}