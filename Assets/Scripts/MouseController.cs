using System;
using Objects.Player;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    public Camera mainCamera;
    public Ball ball;
    public Hud hud;

    public Vector3 hover;
    private bool _holding;
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
            if (_holding)
            {
                mousePosition.z = mainCamera.transform.position.y;
                hover = mainCamera.ScreenToWorldPoint(mousePosition);
                _holding = false;
                ball.PlanTrajectory(hover, holdingTime);
                GetComponentInChildren<PlayerController>().disableScroll = false;
                hud.AddShot();
                if (ball.inOrbitAround)
                {
                    var distance = Helper.DistanceToFillFrustum(GetComponentInChildren<PlayerController>().playerCamera, Vector2.one * (ball.inOrbitAround.radiusGravity * 2));
                    GetComponentInChildren<SmoothCamera>().SetZoomTarget(distance);
                }
                ball.SubmitPlan();
            }
            else
            {
                _holding = true;
                holdingTime = 0;
                ball.StartPlanning();
                if (ball.velocity.sqrMagnitude == 0)
                {
                    holdingTime = 5f;
                }
                if (ball.inOrbitAround || ball.velocity.sqrMagnitude == 0)
                {
                    GetComponentInChildren<SmoothCamera>().SetZoomTarget(100);
                }
                else
                {
                    GetComponentInChildren<SmoothCamera>().SetZoomTarget(Math.Min(100 * ball.velocity.magnitude / 15, 140));
                }
                GetComponentInChildren<PlayerController>().disableScroll = true;
            }
        }
        else if (_holding)
        {
            // holdingTime += Time.deltaTime;
            mousePosition.z = mainCamera.transform.position.y;
            hover = mainCamera.ScreenToWorldPoint(mousePosition);
            hover.y = 0;

            var vv = ball.velocity.magnitude;
            // holdingTime += Input.mouseScrollDelta.y * Time.deltaTime * (vv == 0 ? 10 : vv);
            holdingTime += (float) Math.Round(Input.mouseScrollDelta.y * Time.deltaTime * (vv == 0 ? 10 : vv) /5, 2);
        }

        if (Input.GetButtonDown("Fire2"))
        {
            if (ball.HasPlan()) // Abort with R-Click
            {
                ball.ScrapPlan();
                _holding = false;
                GetComponentInChildren<PlayerController>().disableScroll = false;
            }
            else
            {
                ball.EngangeBrakes();
            }
        }

        if (Input.GetButtonDown("BackSpace"))
        {
            ball.ScrapPlan();
            _holding = false;
            ball.Revive();
        }

        if (_holding)
        {
            ball.PlanTrajectory(hover, holdingTime);
        }
    }

}