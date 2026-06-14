using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovimentoJogador : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    [Header("Status do Jogador")]
    public float vidaTotal = 100f;
    private bool estaMorto = false;
    private bool estaAtacando = false;
    private bool estaAtordoado = false;

    [Header("Efeito de Dano")]
    public Material materialFlash; 
    private Material materialOriginal;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        materialOriginal = sr.material;
    }

    void Update()
    {
        if (estaMorto || estaAtordoado)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        rb.velocity = moveInput * moveSpeed; 

        // --- BOTOES DE TESTE (Apenas para desenvolvimento) ---
        
        // Aperte 'H' para causar 25 de dano no próprio jogador
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Player tomou dano para teste!");
            ReceberDano(25f);
        }
    }

    // Método já chamado pelo Input System para Andar
    public void Move(InputAction.CallbackContext context)
    {
        // Se estiver travado, ignora a leitura do controle/teclado
        if (estaMorto || estaAtacando || estaAtordoado) return;

        // 1. Lê a direção que o jogador está apertando agora
        moveInput = context.ReadValue<Vector2>();   

        // 2. Se a direção for maior que zero (ou seja, ele está apertando algo)
        if (moveInput.sqrMagnitude > 0) 
        {
            animator.SetBool("isWalking", true);
            
            // Salva a direção na memória IMEDIATAMENTE e o tempo todo
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
        }
        else 
        {
            // Se for zero (soltou o controle), ele para, mas NÃO apaga a memória!
            animator.SetBool("isWalking", false);
        }

        // 3. Alimenta o Blend Tree de caminhada normalmente
        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);
    }

    // --- LÓGICA DE COMBATE ABAIXO ---

    // 1. Método para ser chamado pelo Input System (Ex: Botão de Tiro/Espada)
    public void Atacar(InputAction.CallbackContext context)
    {
        // context.started garante que ele só ataque 1 vez quando você apertar o botão (não metralha se segurar)
        if (context.started && !estaAtacando && !estaMorto && !estaAtordoado)
        {
            StartCoroutine(RotinaAtaque());
        }
    }

    // 2. Método para receber dano dos inimigos (Slime/Boss vão chamar isso)
    public void ReceberDano(float dano)
    {
        if (estaMorto || estaAtordoado) return; // O atordoado dá aquele "i-frame" (invencibilidade temporária)
        
        vidaTotal -= dano;
        StartCoroutine(RotinaFlash());
        
        if (vidaTotal <= 0)
        {
            StartCoroutine(RotinaMorte());
        }
        else
        {
            animator.SetTrigger("Hurt");
            StartCoroutine(RotinaAtordoamento());
        }
    }

    // --- CORROTINAS ---
    private IEnumerator RotinaAtaque()
    {
        estaAtacando = true;
        moveInput = Vector2.zero; // Zera a intenção de movimento
        animator.SetBool("isWalking", false);
        
        animator.SetTrigger("Attack");

        // Tempo do ataque (ajuste de acordo com a sua animação do player)
        yield return new WaitForSeconds(0.4f); 

        estaAtacando = false;
    }

    private IEnumerator RotinaFlash()
    {
        sr.material = materialFlash;
        yield return new WaitForSeconds(0.1f);
        sr.material = materialOriginal;
    }

    private IEnumerator RotinaAtordoamento()
    {
        estaAtordoado = true;
        moveInput = Vector2.zero;
        yield return new WaitForSeconds(0.3f); // Tempo travado após tomar um hit
        estaAtordoado = false;
    }

    private IEnumerator RotinaMorte()
    {
        estaMorto = true;
        moveInput = Vector2.zero;
        rb.isKinematic = true; 

        animator.SetTrigger("Death");

        // Aqui você pode colocar uma tela de Game Over depois de uns segundos
        yield return new WaitForSeconds(2.0f);
        
        // Destroy(gameObject); // Normalmente a gente não destrói o player, mas chama um menu.
    }
}