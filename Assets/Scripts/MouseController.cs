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
            ball.Freeze();
        }
        else if (Input.GetButtonUp("Fire1") && holding)
        {
            mousePosition.z = mainCamera.transform.position.y;
            hover = mainCamera.ScreenToWorldPoint(mousePosition);
            holding = false;
            ball.Bump(-BumpSpeed(ball.transform.position, ball.velocity, maxBumpSpeed));
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
                ball.UnFreeze();
            }
            else
            {
                ball.EngangeBreaks();
            }
            
        }
        
        if (holding)
        {
            // TODO draw it
        }
    }

    private void OnDrawGizmos()
    {
        if (holding)
        {
            var ballPos = ball.transform.position;

            var bumbSpeed = BumpSpeed(ballPos, ball.velocity, maxBumpSpeed);
            // Gizmos.color = Color.white;
            // Gizmos.DrawLine(ballPos, ballPos + bumbSpeed * 3);
            // Gizmos.color = Color.yellow;
            // Gizmos.DrawLine(ballPos, hover);

            var pseudoDt = 0.01f;
            var v = ball.velocity - bumbSpeed;
            for (var i = 0; i < 2000; i++)
            {
                var acceleration = Vector3.zero;
                foreach (var planet in World.allPlanets)
                {
                    var delta = ballPos - planet.transform.position;
                    if (GravityObject.CheckCollided(delta, planet.radius + ball.radius))
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(ballPos, ball.radius);
                        return;
                    }
                    acceleration -= GravityObject.CalcGravityAcceleration(delta, ball.mass, planet);
                }

                v += acceleration * pseudoDt;
                ballPos += v * pseudoDt;
                
                if (i % 10 == 0)
                {
                    Handles.color = Color.white;
                    Handles.DrawWireDisc(ballPos, Vector3.up, ball.radius);    
                }
            }
        }
    }

    private Vector3 BumpSpeed(Vector3 ballPos, Vector3 ballVelocity, float maxSpeed)
    {
        var minSpeed = 1.2f;

        var playerControlledDirection = -(hover - ballPos).normalized;
        var ballControlledDirection = -ballVelocity.normalized;

        // Linear curve
        var linearMagnitude = holdingTime * maxSpeed;

        // Sinus curve with min speed
        var p = 5;
        var triangleMagnitude = (float) (2 * Math.Abs(2 * ((holdingTime / p) - Math.Floor((holdingTime / p) + 0.5)))) * (maxSpeed-minSpeed)/2 + minSpeed;
        var sinusMagnitude = (float) (Math.Sin(0.6 * holdingTime - 1.57) * (maxSpeed - minSpeed) / 2 + (maxSpeed / 2 + minSpeed));

        // var magnitude = Math.Min(linearMagnitude, 5);
        if (ballVelocity.sqrMagnitude == 0)
        {
            return playerControlledDirection * Math.Min(minSpeed + linearMagnitude, maxSpeed);
        }

        return ballControlledDirection * Math.Min(triangleMagnitude, maxSpeed);

    }
}
