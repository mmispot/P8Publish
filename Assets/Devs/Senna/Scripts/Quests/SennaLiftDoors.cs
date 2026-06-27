using UnityEngine;

namespace P8Publish.Quests
{
    // Drives the lift door Animator based on player proximity.
    // Requires an Animator with a bool parameter (default "IsOpen").
    // Use Tools > Senna > Setup Lift Animator to create the controller automatically.
    public class SennaLiftDoors : MonoBehaviour
    {
        [SerializeField] private Animator doorAnimator;
        [SerializeField] private string isOpenParam = "IsOpen";

        [Header("Detection")]
        [SerializeField] private Transform player;
        [SerializeField] private float openRange = 3f;

        private int _isOpenHash;

        private void Awake()
        {
            _isOpenHash = Animator.StringToHash(isOpenParam);
        }

        private void Update()
        {
            if (doorAnimator == null || player == null) return;
            bool inRange = Vector3.Distance(transform.position, player.position) <= openRange;
            doorAnimator.SetBool(_isOpenHash, inRange);
        }
    }
}
