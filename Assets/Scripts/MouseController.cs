using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    public Camera mainCamera;
    public Ball ball;

    public Vector3 hover;
    private bool holding;

    void Update()
    {
        var mousePosition = Input.mousePosition;
        if (Input.GetButtonDown("Fire1"))
        {
            mousePosition.z = mainCamera.transform.position.y;
            holding = true;
            ball.frozen = true;
        }
        else if (Input.GetButtonUp("Fire1"))
        {
            mousePosition.z = mainCamera.transform.position.y;
            hover = mainCamera.ScreenToWorldPoint(mousePosition);
            holding = false;
            var dv = (ball.transform.position - hover);
            dv.y = 0;
            ball.velocity += dv;
            ball.frozen = false;
        }
        else if (holding)
        {
            mousePosition.z = mainCamera.transform.position.y;
            hover = mainCamera.ScreenToWorldPoint(mousePosition);
            hover.y = 0;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        var transformPosition = ball.transform.position;
        if (holding)
        {
            Gizmos.DrawLine(transformPosition, hover);
        }
    }
}
