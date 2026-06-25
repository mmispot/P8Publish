using UnityEngine;

namespace P8Publish.Quests
{
    // Attach to the lift object.
    // Assign the PARENT of each door group (e.g. L_middel, L_side) — their children move with them automatically.
    // Adjust leftOpenOffset / rightOpenOffset in the inspector until the doors slide the right way.
    public class SennaLiftDoors : MonoBehaviour
    {
        [Header("Left doors")]
        [SerializeField] private Transform[] leftDoors;
        [Tooltip("Local-space offset applied when open. Flip sign if sliding wrong way.")]
        [SerializeField] private Vector3 leftOpenOffset = new Vector3(-1.2f, 0f, 0f);

        [Header("Right doors")]
        [SerializeField] private Transform[] rightDoors;
        [Tooltip("Local-space offset applied when open. Flip sign if sliding wrong way.")]
        [SerializeField] private Vector3 rightOpenOffset = new Vector3(1.2f, 0f, 0f);

        [Header("Slide settings")]
        [SerializeField] private float speed = 2f;

        [Header("Detection")]
        [SerializeField] private Transform player;
        [SerializeField] private float openRange = 3f;

        private Vector3[] _leftClosed;
        private Vector3[] _rightClosed;
        private bool _open;

        private void Awake()
        {
            _leftClosed  = new Vector3[leftDoors.Length];
            _rightClosed = new Vector3[rightDoors.Length];

            for (int i = 0; i < leftDoors.Length;  i++) _leftClosed[i]  = leftDoors[i].localPosition;
            for (int i = 0; i < rightDoors.Length; i++) _rightClosed[i] = rightDoors[i].localPosition;
        }

        private void Update()
        {
            if (player != null)
                _open = Vector3.Distance(transform.position, player.position) <= openRange;

            for (int i = 0; i < leftDoors.Length; i++)
            {
                var target = _open ? _leftClosed[i] + leftOpenOffset : _leftClosed[i];
                leftDoors[i].localPosition = Vector3.MoveTowards(leftDoors[i].localPosition, target, speed * Time.deltaTime);
            }

            for (int i = 0; i < rightDoors.Length; i++)
            {
                var target = _open ? _rightClosed[i] + rightOpenOffset : _rightClosed[i];
                rightDoors[i].localPosition = Vector3.MoveTowards(rightDoors[i].localPosition, target, speed * Time.deltaTime);
            }
        }
    }
}
