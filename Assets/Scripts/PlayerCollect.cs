using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
[RequireComponent(typeof(AudioSource))]
public class PlayerCollect : MonoBehaviour
{
    [Header("sound")]
    private AudioSource audioSource;
    public AudioClip collectSound; // 跳跃音效
    public AudioClip winSound; // win音效
    public LayerMask geerLayers;
    private RaycastHit geerHit;
    [SerializeField]
    private int Num = 0;
    public TextMeshProUGUI scoreText;
    public GameObject winGamePanel;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void GeerCheck()
    {
        //设置地面检测球体位置，带偏移
        bool geerCheck = Physics.SphereCast(transform.position, 1.0f,transform.up,  out geerHit, 2.0f, geerLayers);
        if(geerCheck)
        {
            audioSource.PlayOneShot(collectSound);
            Num++;
            SetScoreText();

            Destroy(geerHit.transform.gameObject);
            Debug.Log(Num);
        }
    }
    void Start()
    {
        SetScoreText();
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            Num = 0;
            SetScoreText();
        }
        GeerCheck();
        WinGame();
    }
    public void SetScoreText()
    {
        scoreText.text = Num.ToString()+" / 40";
    }

    public void WinGame()
    {
        if (Num == 40)
        {
            winGamePanel.SetActive(true);
            //audioSource.PlayOneShot(winSound);
        }
    }

    private void OnDrawGizmos()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Gizmos.color = transparentGreen;
 
        Gizmos.DrawSphere(transform.position, 0.5f);
    }   
}
