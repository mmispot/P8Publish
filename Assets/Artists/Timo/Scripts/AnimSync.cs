using UnityEngine;

public class AnimSync : MonoBehaviour
{
    public Animator armsAnimator;
    public Animator gunAnimator;

    void Start()
    {
        armsAnimator.Play("ArmsTT33_Idle", 0, 0f);
        gunAnimator.Play("GunIdle", 0, 0f);
    }

    void Update()
    {
        // Keep gun animator locked to arms animator normalized time
        float normalizedTime = armsAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        gunAnimator.Play("GunIdle", 0, normalizedTime % 1f);
    }
}