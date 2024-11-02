using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMotion : MonoBehaviour
{
    // Start is called before the first frame update
    private Vector3 initialPos = Vector3.zero;
    private Quaternion initialRot = Quaternion.identity;
    [SerializeField] private float rotationSpeed = 10;
    [SerializeField] private float frequency = 5;
    [SerializeField] private float amplitude = 1;
    [SerializeField] private float minRotAngle = 0.1f;
    void Start()
    {
        initialPos = transform.position;
        initialRot = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = initialPos +  Vector3.right * (amplitude * Mathf.Sin( frequency * Time.time));
        //var quat = Quaternion.AngleAxis(Time.time * rotationSpeed, transform.forward);
        transform.Rotate(Vector3.forward * (Time.deltaTime * rotationSpeed), Space.Self);
        //transform.rotation = quat * initialRot;
    }
}
