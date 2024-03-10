using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    LedgeClimbController ledgeClimbController;

    [Header("sound")]
    private AudioSource audioSource;

    public AudioClip jumpSound; // 跳跃音效

    [Header("Player")]
    public CharacterController controller;
    public Transform climbCheckRoot;
    public float MoveSpeed;
    //public float SprintSpeed = 5.335f;
    public float SpeedChangeRate = 10.0f;
    public float RotationSmoothTime = 0.12f;


    [Header("InputSystem")]
    private ClimbingSystem inputControl;
    public Vector2 inputDirection;
    public Camera mainCamera;

    [Header("Player Grounded")]
    public bool Grounded = true;
    public float GroundedOffset = -0.2f;
    public float GroundedRadius = 0.3f;
    public LayerMask GroundLayers;

    [Header("Player Jump")]
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    [Space(10)]
    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;

    // player
    private float _speed;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;

    [Header("particle")]
    public ParticleSystem smoke1;
    public ParticleSystem smoke2;
    //Animation
    private Animator animator;
    public bool isJump = false;

    private void Awake()
    {
        inputControl = new ClimbingSystem();

        controller = GetComponent<CharacterController>();

        ledgeClimbController = GetComponent<LedgeClimbController>();

        smoke1 = GetComponentInChildren<ParticleSystem>();
        smoke2 = GetComponentInChildren<ParticleSystem>();

        audioSource = GetComponent<AudioSource>();
        mainCamera = Camera.main;
    }
    void OnEnable()
    {
        inputControl.Enable();
    }
    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    private void OnDisable()
    {
        inputControl.Disable();
    } 
    private void Update()
    {
        //check if climbing. if climbing, gravity = 0
        if(ledgeClimbController.isGrabing)
        {
            Gravity = 0.0f;
        }
        else
        {
            Gravity = -15.0f;
        }

        //check if ledge
        ledgeClimbController.LedgeCheck();

        //move and jump
        //Move();
        Jump();

        ledgeClimbController.LedgeRelease();
        GroundedCheck(); 
    }
    private void FixedUpdate()
    {
        //check if climbing. if climbing, gravity = 0
        Move();

    }

    private void GroundedCheck()
    {
        //set the position of the ground detection sphere, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void Move()
    {
        if(ledgeClimbController.isGrabing)
        {
            //climbmove
            ledgeClimbController.LedgeMove();
        }
        else
        {
            GroundMove();
        }
    }
    private void Jump()
    {
        Debug.Log(ledgeClimbController.isGrabing);
        if(ledgeClimbController.isGrabing)
        {
            
            ledgeClimbController.LedgeClimb();
        }
        else
        {
            if(ledgeClimbController.isLedge)
                ledgeClimbController.EnterLedge();
            else
                GroundJump();
        }
        
    }
    public void GroundMove()
    {
        float targetSpeed = MoveSpeed;
        
        Vector2 moveMentInput = inputControl.GamePlay.Move.ReadValue<Vector2>();
        Vector3 move = new Vector3(moveMentInput.x, 0.0f, moveMentInput.y);

        if (moveMentInput == Vector2.zero) targetSpeed = 0.0f;
        float currentHorizontalSpeed = new Vector3(move.x, 0.0f, move.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        // normalise input direction
        Vector3 inputDirection = new Vector3(moveMentInput.x, 0.0f, moveMentInput.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        //rotate smothly
        if (moveMentInput != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        //move
        controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        
        animator.SetFloat("speed", _speed);
        if(_speed>0.1f)
        {
            smoke1.Play();
            smoke2.Play();
        }
        else
        {
            smoke1.Stop();
            smoke2.Stop();
        }
    }

    private void GroundJump()
    {
        if(Grounded && _verticalVelocity < 0.0f)
        {
            _verticalVelocity = -2.0f;
            isJump = false;
            animator.SetBool("jump", isJump);
        }
        if(Grounded && inputControl.GamePlay.Jump.triggered)
        {
            isJump = true;   
            _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

            //动画 音效
           animator.SetBool("jump", isJump);
           audioSource.PlayOneShot(jumpSound);
        }
        _verticalVelocity += Gravity * Time.deltaTime;
        //animator.SetBool("jump", isJump);
        //Debug.Log(_verticalVelocity);
    }

    private void OnDrawGizmos()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
    }   
}
