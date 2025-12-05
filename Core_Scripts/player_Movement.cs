
using UnityEngine;
using UnityEngine.InputSystem;
using Terresquall;


[RequireComponent(typeof(CharacterController))]

public class player_Movement : MonoBehaviour
{
    private Vector2 _input;
    private CharacterController _CharacterController;
    private Vector3 _direction;
    [SerializeField] private float _speed;
    [SerializeField] private float smoothtime = 0.05f;
    [SerializeField] private Animator _Animator;
    [SerializeField] private VirtualJoystick joystick;

    private float _currentVelocity;
    

    private void Awake()
    {
        //_Animator = GetComponent<Animator>();
        _CharacterController = GetComponent<CharacterController>();
        
    }
    private void Update()
    {
        // 1. Read input from the Virtual Joystick
        _input = new Vector2(joystick.axis.x, joystick.axis.y);

        // 2. Calculate direction and update Animator
        _Animator.SetFloat("Mag", _input.magnitude);
        _direction = new Vector3(_input.x, 0.0f, _input.y).normalized; // Use normalized for consistent speed

       

        // 3. Apply Rotation and Movement

        ApplyRotation();
        ApplyMovement();
    }
    private void ApplyRotation()
    {
        if (_input.sqrMagnitude == 0) return;
        var targetAngle = Mathf.Atan2(_direction.x, _direction.z) * Mathf.Rad2Deg;
        var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothtime);
        transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
    }
    private void ApplyMovement()
    {
        _CharacterController.Move(_direction * _speed * Time.deltaTime);
      //  _CharacterController.Move(_direction_mobile * _speed * Time.deltaTime);

    }
    //public void Move(InputAction.CallbackContext context)
    //{
    //     _input = context.ReadValue<Vector2>();
    //    //  _input = new Vector2(joystick.axis.x, joystick.axis.y);

    //    _Animator.SetFloat("Mag", _input.magnitude);
    //    _direction = new Vector3(_input.x, 0.0f, _input.y);

    //    // _direction_mobile = new Vector3(VirtualJoystick.GetAxisRaw("Horizontal"), 0.0f, VirtualJoystick.GetAxisRaw("Vertical"));
    //    //Debug.Log(_direction_mobile);
    //}

}
