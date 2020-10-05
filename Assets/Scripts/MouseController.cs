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
        if (!ball)
        {
            return;
        }
        
        var mousePosition = Input.mousePosition;
        if (Input.GetButtonDown("Fire3"))
        {
            mousePosition.z = mainCamera.transform.position.y;
            ball.CheatJumpTo(mainCamera.ScreenToWorldPoint(mousePosition));
        }
        else if (Input.GetButtonDown("Fire1"))
        {
            if (ball.HasPlan())
            {
                ball.SubmitPlan();
            }
            else
            {
                holding = true;
                holdingTime = 0;
                ball.StartPlanning();
            }
        }
        else if (Input.GetButtonUp("Fire1") && holding)
        {
            mousePosition.z = mainCamera.transform.position.y;
            hover = mainCamera.ScreenToWorldPoint(mousePosition);
            holding = false;
            ball.PlanTrajectory(hover, holdingTime);
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
            if (ball.HasPlan()) // Abort with R-Click
            {
                ball.ScrapPlan();
            }
            else
            {
                ball.EngangeBreaks();
            }
        }

        if (Input.GetButtonDown("BackSpace"))
        {
            ball.Revive();
        }

        if (holding)
        {
            ball.PlanTrajectory(hover, holdingTime);
        }
    }

}