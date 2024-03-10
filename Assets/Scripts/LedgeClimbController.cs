using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
[RequireComponent(typeof(AudioSource))]
public class LedgeClimbController : MonoBehaviour
{
    [Header("sound")]
    private AudioSource audioSource;

    public AudioClip jumpSound; // Jump sound effect


    [Header("Player")]
    public CharacterController controller;
    public Transform climbCheckRoot;
    public Transform climbUpCheckRoot;
    public Transform leftHand;
    public Transform rightHand;
    public Vector3 climbPosition;
    public Vector3 climbUpPosition;
    [Header("InputSystem")]
    private ClimbingSystem inputControl;
    public float ledgeMoveSpeed = 3.0f;


    [Header("Ledge Check")]
    public LayerMask ledgeLayer;
    public LayerMask climbUpLayer;
    public bool isLedge = false;//check if is ledge
    
    public bool isGrabing = false;//check if is grabing
    public float ledgeCheckDistance = 4.0f;
    public float ledgeCheckRadius = 1f;
    public float maxDistance = 6.0f;
    private Vector3 ledgeRotateMovePosition;
    private RaycastHit ledgeHit;
    //private RaycastHit checkFowardHit;
    private RaycastHit checkLeftHit;
    private RaycastHit checkRightHit;
    private RaycastHit climbUpHit;

    [Header("ClimbPosiontOffest")]
    public float climbPositionOffsetY = 2.6f;
    public float climbPositionOffsetZ = 1.6f;
    private float climbPositionOffsetX;

    [Header("Timer")]
    public float ReloadTimer; // seconds
    public float ReloadTime; // seconds

    //player
    private Animator animator;
    private float _speed;
    [SerializeField]
    private float _ledgeClimbSpeed = 5.0f;//ledge climb speed
    public float _ledgeClimbHeight = 4f;//ledge climb height
    public float Gravity = -15.0f;




    private void Awake()
    {
        inputControl = new ClimbingSystem();
        controller = GetComponent<CharacterController>();

        audioSource = GetComponent<AudioSource>();

        animator = GetComponent<Animator>();
    }
    private void OnEnable()
    {
        inputControl.GamePlay.Enable();
    }
    private void OnDisable()
    {
        inputControl.GamePlay.Disable();
    }




    //chegk if is ledge, chack can climb
    public void LedgeCheck()
    {        
        bool ledgeCheck = Physics.SphereCast(climbCheckRoot.position, ledgeCheckRadius, climbCheckRoot.up, out ledgeHit, ledgeCheckDistance, ledgeLayer);
        if (!ledgeCheck)
        {
            isLedge = false;
            return;
        }
        float checkDistance = Vector3.Distance(climbCheckRoot.position, ledgeHit.transform.position);
        //Debug.Log(checkDistance);
        if (checkDistance < maxDistance)
        {
            isLedge = true;
        }
        else
        {
            isLedge = false;
        }
    }

    public void CheckFoward()
    {
        //check both hand foward
        bool ledgeCheckLeft = Physics.Raycast(leftHand.position, leftHand.forward, out checkLeftHit, ledgeCheckDistance, ledgeLayer);
        Debug.DrawRay(leftHand.position, leftHand.forward * ledgeCheckDistance, Color.red);
        bool ledgeCheckRight = Physics.Raycast(rightHand.position, rightHand.forward, out checkRightHit, ledgeCheckDistance, ledgeLayer);
        Debug.DrawRay(rightHand.position, rightHand.forward * ledgeCheckDistance, Color.red);
        if(!ledgeCheckLeft && !ledgeCheckRight)
        {
            LedgeRelease();
        }
    }

    public bool CheckClimbUp()
    {
        bool CheckClimbUp = Physics.Raycast(climbUpCheckRoot.position, climbUpCheckRoot.forward, out climbUpHit, 1.5f, climbUpLayer);
        Debug.DrawRay(climbUpCheckRoot.position, climbUpCheckRoot.forward * ledgeCheckDistance, Color.red);
        return CheckClimbUp;
        
        //Debug.Log(climbUpHit.transform.gameObject.layer);
    }



    //enter ledgeclimbing state setting isledge isgrabing
    public void EnterLedge()
    {
        if (isLedge && isGrabing == false && inputControl.GamePlay.Jump.triggered)
        {
            Debug.Log("Climb");
            animator.SetBool("climbledge", true);


            climbPositionOffsetX = Vector3.Dot(ledgeHit.transform.position- transform.position,ledgeHit.transform.right);
            climbPosition = ledgeHit.transform.position - ledgeHit.transform.right * climbPositionOffsetX - ledgeHit.transform.up * 2.0f - ledgeHit.transform.forward * 1.3f;
            //climbPosition = new Vector3(climbCheckRoot.transform.position.x, ledgeHit.transform.position.y - climbPositionOffsetY, ledgeHit.transform.position.z - climbPositionOffsetZ);
            
            //lerp to climb position
            transform.position = Vector3.Lerp(transform.position, climbPosition, Time.deltaTime * 10.0f);
            transform.forward = ledgeHit.transform.forward;
            //controller.Move(climbPosition - transform.position);
            StartCoroutine(LerpLedgeJump(climbPosition));
            isGrabing = true;
        }
    }




    public void LedgeMove()
    {
        Debug.Log("ClimbMove");
        
        float targetSpeed = ledgeMoveSpeed;
        Vector2 moveMentInput = inputControl.GamePlay.Move.ReadValue<Vector2>();


        //check both hand foward
        //if one hand's foward is null, stop move
        CheckFoward();
        if(checkLeftHit.transform == null && moveMentInput.x < 0)
        //cant move left
            moveMentInput.x = 0;
        
        if(checkRightHit.transform == null && moveMentInput.x > 0)
        //cant move right
            moveMentInput.x = 0;


        //move direction is plyer's right
        Vector3 move = transform.right * moveMentInput.x;



        //Debug.Log(move);
        if (moveMentInput == Vector2.zero) 
            targetSpeed = 0.0f;


        float currentHorizontalSpeed = new Vector3(move.x, 0.0f, move.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * 10f);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        //set ledge move animation
        if(moveMentInput.x != 0)
            animator.SetBool("ledgeMove", true);
        else
            animator.SetBool("ledgeMove", false);


        //climb turn left
        if(checkLeftHit.transform != null && checkLeftHit.transform.forward != transform.forward)
        {
                //startcouritne
                Debug.Log("left");
                //float ledgeOffset = checkRightHit.transform.position.z-transform.position.z;
                //float handOffset = -leftHand.position.x + transform.position.x;
                //ledgeRotateMovePosition = new Vector3(transform.position.x-ledgeOffset-handOffset, transform.position.y, transform.position.z+ledgeOffset+handOffset);
                float ledgeOffset = Vector3.Dot(checkRightHit.transform.position- transform.position,transform.forward);
                float handOffset = -Vector3.Dot(checkRightHit.transform.position- transform.position,transform.right);
                Debug.Log(ledgeOffset);
                Debug.Log(handOffset);
                ledgeRotateMovePosition = transform.position + transform.forward * (ledgeOffset+handOffset) - transform.right * (handOffset+ledgeOffset);
                //float t = Time.deltaTime * 2;
               //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(new Vector3(0, 90, 0)), t);
                StartCoroutine(LedgeRotateMove(ledgeRotateMovePosition, checkLeftHit.transform));    
        } 

        //climb turn right
        if(checkRightHit.transform != null && checkRightHit.transform.forward != transform.forward)
        {
                //startcouritne
                Debug.Log("Right");
                //float ledgeOffset = checkRightHit.transform.position.z-transform.position.z;
                //float handOffset = -leftHand.position.x + transform.position.x;
                //ledgeRotateMovePosition = new Vector3(transform.position.x-ledgeOffset-handOffset, transform.position.y, transform.position.z+ledgeOffset+handOffset);
                float ledgeOffset = Vector3.Dot(checkLeftHit.transform.position- transform.position,transform.forward);
                float handOffset = Vector3.Dot(checkRightHit.transform.position - transform.position,transform.right);
                //Debug.Log(ledgeOffset);
                Debug.Log(handOffset);
                ledgeRotateMovePosition = transform.position + transform.forward * (ledgeOffset+handOffset) + transform.right * (handOffset+ledgeOffset);
                //float t = Time.deltaTime * 2; 
               //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(new Vector3(0, 90, 0)), t);
                StartCoroutine(LedgeRotateMove(ledgeRotateMovePosition, checkRightHit.transform));    
        } 
        controller.Move(move*(_speed * Time.deltaTime));
    }


    //climb turn IEnumerator,move and rotate to climb position
    //make player face to ledge
    //BUG only turn left or right
    IEnumerator LedgeRotateMove(Vector3 target, Transform hit)
    {
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            yield return null;
            transform.position = Vector3.Slerp(transform.position, target, Time.deltaTime * 5.0f);
            transform.forward = hit.forward;
            //transform.rotation = Quaternion.Slerp(transform.rotation, hit.rotation, Time.deltaTime * 5.0f);
            //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0, 90, 0)), t);
            controller.Move((target - transform.position).normalized * _ledgeClimbSpeed * Time.deltaTime);
        }
    }



    //climb jump
   public void LedgeClimb()
   {
        //timer jump every 0.5s
        ReloadTimer -= Time.deltaTime;
        CheckClimbUp();

        if (ReloadTimer > 0)
            return;
        if(inputControl.GamePlay.Jump.triggered && isLedge)
        {
            //jummp sound
            audioSource.PlayOneShot(jumpSound);

            ReloadTimer = ReloadTime;
            Debug.Log("LedgeJump");
            animator.SetBool("ledgejump", true);
            climbPositionOffsetX = Vector3.Dot(ledgeHit.transform.position- transform.position,ledgeHit.transform.right);
            climbPosition = ledgeHit.transform.position - ledgeHit.transform.right * climbPositionOffsetX - ledgeHit.transform.up * 2.0f - ledgeHit.transform.forward * 1.3f;
            // climbPositionOffsetX = Vector3.Dot(ledgeHit.transform.position- transform.position,ledgeHit.transform.forward);
            // climbPosition = ledgeHit.transform.position - ledgeHit.transform.forward*climbPositionOffsetX + ledgeHit.transform.right * climbPositionOffsetY + ledgeHit.transform.up * climbPositionOffsetZ;
            //climbPosition = new Vector3(transform.position.x, ledgeHit.transform.position.y - climbPositionOffsetY, ledgeHit.transform.position.z - climbPositionOffsetZ);
            StartCoroutine(LerpLedgeJump(climbPosition));
        }
        //climb up to the roof
        if(inputControl.GamePlay.Jump.triggered && !CheckClimbUp() && isGrabing)
        {
            Debug.Log("ClimbUp");
            LedgeClimbUp();
        }
   }
   //LedgeJumm IEnumerator to jump to target position
   IEnumerator LerpLedgeJump(Vector3 target)
   {
       while(Vector3.Distance(transform.position, target) > 0.1f)
        {
            yield return null;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 5.0f);
            controller.Move((target - transform.position).normalized * _ledgeClimbSpeed * Time.deltaTime);
        }
        animator.SetBool("ledgejump", false);
   }


   //right mouse button to release ledge
   public void LedgeRelease()
   {
        if(inputControl.GamePlay.Release.triggered)
        {
            Debug.Log("Release");
            animator.SetBool("climbledge", false);
            isGrabing = false;
            animator.SetBool("climbledge", false);
        }
   }




   public void LedgeClimbUp()
   {
        audioSource.PlayOneShot(jumpSound);

        Debug.Log("ClimbUp");
        animator.SetBool("climbup", true);
        climbUpPosition = transform.position + transform.up * 2.7f + transform.forward * 1.0f;
        StartCoroutine(LerpLedgeClimbUp(climbUpPosition));
   }
    IEnumerator LerpLedgeClimbUp(Vector3 target)
    {
        while(Vector3.Distance(transform.position, target) > 0.1f)
        {
            yield return null;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 5.0f);
            controller.Move((target - transform.position).normalized * _ledgeClimbSpeed * Time.deltaTime);
        }
        animator.SetBool("climbup", false);
        // animator.SetBool("ledgeMove", false);
        animator.SetBool("climbledge", false);
        isGrabing = false;
    }






    private void OnDrawGizmos()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Gizmos.color = transparentGreen;
        
        Gizmos.DrawSphere(new Vector3(climbCheckRoot.transform.position.x, climbCheckRoot.transform.position.y, climbCheckRoot.transform.position.z), ledgeCheckRadius);
        //Gizmos.DrawSphere(new Vector3(ledgeHit.transform.position.x, ledgeHit.transform.position.y, ledgeHit.transform.position.z), 0.7f);
        if(ledgeHit.transform != null)
        Gizmos.DrawSphere(new Vector3(ledgeHit.transform.position.x, ledgeHit.transform.position.y, ledgeHit.transform.position.z), 0.7f);
        //Gizmos.DrawSphere(new Vector3(climbPosition.x, climbPosition.y, climbPosition.z), ledgeCheckRadius);
        //Gizmos.DrawSphere(new Vector3(checkLeftHit.transform.position.x, checkLeftHit.transform.position.y, checkLeftHit.transform.position.z), 1.0f);
    } 
}
