using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerInputActions _input;
    private CharacterController _cha;
    private Vector2 _inputMove;
    private float _inputRotation;
    private bool _running;

    private Vector3 _lastGround;

    private float _speed
    {
        get
        {
            return _running ? 3f : 1f;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        _cha = GetComponent<CharacterController>();

        _input = new PlayerInputActions();
        _input.Player.Enable();
        _input.Player.Walk.performed += Walk_performed;
        _input.Player.Walk.canceled += Walk_canceled;
        _input.Player.Run.performed += Run_performed;
        _input.Player.Run.canceled += Run_canceled;
        _input.Player.Turn.performed += Turn_performed;
        _input.Player.Turn.canceled += Turn_canceled;
    }

    // Update is called once per frame
    void Update()
    {
        if(_cha.isGrounded)
            _lastGround = transform.position;

        if(transform.position.y - _lastGround.y < -10.0f)
        {
            Debug.Log("I'm falling!");
            transform.position = new Vector3(0.0f, 1.5f, 0.0f);
        }
        else if(!_cha.isGrounded || _inputMove != Vector2.zero || _inputRotation != 0.0f)
        {
            Movement();
        }
    }

    private void Walk_performed(InputAction.CallbackContext obj)
    {
        _inputMove = obj.ReadValue<Vector2>();
    }

    private void Walk_canceled(InputAction.CallbackContext obj)
    {
        _inputMove = Vector2.zero;
    }

    private void Run_performed(InputAction.CallbackContext obj)
    {
        _running = true;
    }

    private void Run_canceled(InputAction.CallbackContext obj)
    {
        _running = false;
    }

    private void Turn_performed(InputAction.CallbackContext obj)
    {
        _inputRotation = obj.ReadValue<float>();
    }

    private void Turn_canceled(InputAction.CallbackContext obj)
    {
        _inputRotation = 0.0f;
    }

    private void Movement()
    {
        Vector3 move_speed = new Vector3(_inputMove.x, 0, _inputMove.y) * _speed;
        _cha.SimpleMove(transform.rotation * move_speed);

        transform.Rotate(new Vector3(0.0f, _inputRotation * _speed, 0.0f));
    }
}
