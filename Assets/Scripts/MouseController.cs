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
    
    void Update()
    {
        var mousePosition = Input.mousePosition;
        if (Input.GetButtonDown("Fire3"))
        {
            mousePosition.z = mainCamera.transform.position.y;
            ball.transform.position = mainCamera.ScreenToWorldPoint(mousePosition);
            ball.velocity = Vector3.zero;
            ball.frozen = true;
        }
        else if (Input.GetButtonDown("Fire1"))
        {
            mousePosition.z = mainCamera.transform.position.y;
            holding = true;
            holdingTime = 0;
            ball.frozen = true;
        }
        else if (Input.GetButtonUp("Fire1"))
        {
            mousePosition.z = mainCamera.transform.position.y;
            hover = mainCamera.ScreenToWorldPoint(mousePosition);
            holding = false;
            var dv = -BumbSpeed(ball.transform.position) * 10;
            dv.y = 0;
            ball.velocity += dv;
            ball.frozen = false;
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
            ball.velocity *= 0.8f;
            ball.breakParticleSystem.Play();
            // TODO bremssound so düsen/gas entweichend
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
            Gizmos.color = Color.white;
            var ballPos = ball.transform.position;

            Gizmos.DrawLine(ballPos, ballPos + BumbSpeed(ballPos) * 3);


            var pseudoDt = 0.1f;
            var v = ball.velocity - BumbSpeed(ballPos) * 10;
            for (var i = 0; i < 100; i++)
            {
                var a = Vector3.zero;
                foreach (var planet in World.allPlanets)
                {
                    var delta = ballPos - planet.transform.position;
                    a -= GravityObject.CalcGravityAcceleration(delta, ball.mass, planet);
                }

                v += a * pseudoDt;
                ballPos += v * pseudoDt;
                
                Handles.color = Color.white;
                Handles.DrawWireDisc(ballPos, Vector3.up, ball.radius);
                
            }
        }
    }

    private Vector3 BumbSpeed(Vector3 ballPos)
    {
        return (hover - ballPos).normalized * ((float) Math.Sin(holdingTime * 4 - 1.57) + 1.2f);
    }
}
