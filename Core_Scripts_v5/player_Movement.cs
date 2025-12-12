
using UnityEngine;
using UnityEngine.InputSystem;
using Terresquall;
using HutongGames.PlayMaker;


[RequireComponent(typeof(CharacterController))]

public class player_Movement : MonoBehaviour
{
    private Vector2 _input;
    private CharacterController _CharacterController;
    private Vector3 _direction;
    [SerializeField] private float _speed;
    private float _currentSpeed;

    [SerializeField] private float _runSpeed;
    [SerializeField] private float smoothtime = 0.05f;
    [SerializeField] private Animator _Animator;
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private GameObject PlayerJoystick;

    [Header("Sprint Settings")]
    [SerializeField] private float _maxSprintTime = 5f; // Total sprint capacity
    [SerializeField] private float _sprintConsumptionRate = 1f; // How fast sprint time decreases (e.g., 1 unit per second)
    [SerializeField] private float _sprintRechargeRate = 1f; // How fast sprint time recharges (e.g., 1 unit per second)
    private float _currentSprintTime; // The actual remaining sprint time
    private bool _isSprinting = false;

    private float _currentVelocity;


    private void Awake()
    {
        //_Animator = GetComponent<Animator>();
        _CharacterController = GetComponent<CharacterController>();
      
        _currentSpeed = _speed;
        // Initialize sprint time
        _currentSprintTime = _maxSprintTime;

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
        HandleSprintLimit();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            MakePlayerDance();
        }
        if (Input.GetKeyUp((KeyCode.Space)))
        {
            StopPlayerDance();
        }
    }
    private void HandleSprintLimit()
    {
        if (_isSprinting)
        {
            // CONSUME SPRINT TIME
            // Decrease the remaining sprint time by the consumption rate
            _currentSprintTime -= _sprintConsumptionRate * Time.deltaTime;

            // Automatically stop running if sprint time runs out
            if (_currentSprintTime <= 0f)
            {
                _currentSprintTime = 0f;
                SpeedNormal(); // Revert to walk speed and animation
            }
        }
        else
        {
            // RECHARGE SPRINT TIME
            // Only recharge if the player is not moving to be more realistic (optional)
            if (_input.sqrMagnitude < 0.1f) // Player is standing still
            {
                _currentSprintTime += _sprintRechargeRate * Time.deltaTime;
            }
            else if (_currentSpeed == _speed) // Player is walking
            {
                // Recharge slower while walking, or keep the rate the same, or don't recharge at all
                _currentSprintTime += (_sprintRechargeRate * 0.5f) * Time.deltaTime; // Example: Half recharge rate while walking
            }

            // Clamp the sprint time to the maximum limit
            _currentSprintTime = Mathf.Clamp(_currentSprintTime, 0f, _maxSprintTime);
        }
    }
    public void MakePlayerDance()
    {
        _Animator.SetBool("isDancing", true);
        PlayerJoystick.SetActive(false);
    }
    public void StopPlayerDance()
    {
        _Animator.SetBool("isDancing", false);
        PlayerJoystick.SetActive(true);
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
        //_CharacterController.Move(_direction * _speed * Time.deltaTime);
        _CharacterController.Move(_direction * _currentSpeed * Time.deltaTime);
        //  _CharacterController.Move(_direction_mobile * _speed * Time.deltaTime);

    }
    public void RunFunction()
    {
        // NEW: Only allow running if we have sprint time remaining
        if (_currentSprintTime > 0f)
        {
            Debug.Log("RunSpeedIncreased");
            _currentSpeed = _runSpeed;
            _Animator.SetBool("isRunning", true);
            _isSprinting = true; // Set flag to true

            _Animator.SetBool("isRunning", true);
        }
        else
        {
            Debug.Log("Cannot sprint: Stamina depleted.");
            // If the player tries to run but has no stamina, force normal speed
            SpeedNormal();
        }
    }

    public void SpeedNormal()
    {
        Debug.Log("SpeedNormal");
        _currentSpeed = _speed;
        _Animator.SetBool("isRunning", false);
        _isSprinting = false; // Set flag to false
        _Animator.SetBool("isRunning", false);

    }

    /*
    //old
    public void RunFunction()
    {
        Debug.Log("RunSpeedIncreased");
        _currentSpeed = _runSpeed;
        _Animator.SetBool("isRunning", true);   
    }
    public void SpeedNormal()
    {
        Debug.Log("SpeedNormal");
        _currentSpeed = _speed;
        _Animator.SetBool("isRunning", false);
    }

    */




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
