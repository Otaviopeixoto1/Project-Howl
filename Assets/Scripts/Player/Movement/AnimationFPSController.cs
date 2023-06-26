using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationFPSController : MonoBehaviour
{
    private Animator animator;

    [SerializeField]
    private int fps = 12;
    
    [SerializeField]
    private float totalAnimationTime = 1.0f;

    private float elapsedTime;
    private float elapsedNormalizedTime;

    void Start()
    {
        animator = GetComponent<Animator>();
        elapsedTime = 0f;
        elapsedNormalizedTime = 0f;
    }
    
    //calculate the total frame independent animation time
    //use an "update coroutine"
    void Update()
    {
        elapsedTime += Time.deltaTime;
        elapsedNormalizedTime = Mathf.RoundToInt((elapsedTime/totalAnimationTime) * fps)/(float)fps;
        
        animator.SetFloat("normalizedRunTime",elapsedNormalizedTime);
        if(elapsedTime >= totalAnimationTime)
        {
            elapsedTime = 0f;
            elapsedNormalizedTime = 0;
        }
    }
}
