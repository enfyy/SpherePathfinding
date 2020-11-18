using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeSphere : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MeshFilter MeshFilter = GetComponent<MeshFilter>();
        Debug.Log(MeshFilter.mesh.uv.Length);
        Debug.Log(MeshFilter.mesh.uv2.Length);
        Debug.Log(MeshFilter.mesh.uv3.Length);
        Debug.Log(MeshFilter.mesh.uv4.Length);
        Debug.Log(MeshFilter.mesh.uv5.Length);
        Debug.Log(MeshFilter.mesh.uv6.Length);
        Debug.Log(MeshFilter.mesh.uv7.Length);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
