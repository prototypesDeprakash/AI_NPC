using UnityEngine;

public class kill_System : MonoBehaviour
{
    [SerializeField] private Animator _Animator;
    [SerializeField] private CharacterController _testCharacter;

    private void OnTriggerEnter(Collider other)
    {
       
    }

    public void kill()
    {
        _Animator.SetBool("death", true);
        _testCharacter.height = 0.03f;
        UnityEngine.Vector3 newCenter = _testCharacter.center;
        newCenter.y += 1.11f; // Use 'f' for float literals in C#
        _testCharacter.center = newCenter;
    }
}
