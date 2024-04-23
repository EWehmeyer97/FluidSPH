using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowSphere : MonoBehaviour
{
    //Draw Bounds
    void OnDrawGizmos()
    {
        var m = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawWireSphere(Vector3.zero, .5f);
        Gizmos.matrix = m;
    }
}
