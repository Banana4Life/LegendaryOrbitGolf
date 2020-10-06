using System;
using System.Collections;
using System.Collections.Generic;
using Objects.Player;
using UnityEditor;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    public Camera mainCamera;
    public Ball ball;
    public Hud hud;

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
            if (holding)
            {
                mousePosition.z = mainCamera.transform.position.y;
                hover = mainCamera.ScreenToWorldPoint(mousePosition);
                holding = false;
                ball.PlanTrajectory(hover, holdingTime);
                GetComponentInChildren<PlayerController>().disableScroll = false;
                hud.AddShot();
                if (ball.inOrbitAround)
                {
                    var distance = Helper.DistanceToFillFrustum(GetComponentInChildren<PlayerController>().playerCamera, Vector2.one * ball.inOrbitAround.radiusGravity *2);
                    GetComponentInChildren<SmoothCamera>().SetZoomTarget(distance);
                }
                ball.SubmitPlan();
            }
            else
            {
                holding = true;
                holdingTime = 0;
                ball.StartPlanning();
                GetComponentInChildren<SmoothCamera>().SetZoomTarget(100);
                GetComponentInChildren<PlayerController>().disableScroll = true;
            }
        }
        else if (holding)
        {
            // holdingTime += Time.deltaTime;
            mousePosition.z = mainCamera.transform.position.y;
            hover = mainCamera.ScreenToWorldPoint(mousePosition);
            hover.y = 0;

            holdingTime += Input.mouseScrollDelta.y * Time.deltaTime * ball.velocity.magnitude;
        }

        if (Input.GetButtonDown("Fire2"))
        {
            if (ball.HasPlan()) // Abort with R-Click
            {
                ball.ScrapPlan();
                holding = false;
            }
            else
            {
                ball.EngangeBrakes();
            }
        }

        if (Input.GetButtonDown("BackSpace"))
        {
            ball.ScrapPlan();
            holding = false;
            ball.Revive();
        }

        if (holding)
        {
            ball.PlanTrajectory(hover, holdingTime);
        }
    }

}