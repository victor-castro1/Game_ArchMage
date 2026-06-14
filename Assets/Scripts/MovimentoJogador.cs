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

    [Header("Dash do Jogador")]
    public float velocidadeDash = 15f;
    public float tempoDash = 0.2f;
    public float tempoDuploClique = 0.3f; // Tempo máximo entre os cliques para ativar o dash
    private bool estaDandoDash = false;
    private float ultimoTempoAperto;
    private Vector2 ultimaDirecaoApertada;

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
        // 🚨 TRAVA DE MOVIMENTO
        if (estaMorto || estaAtacando || estaAtordoado || estaDandoDash)
        {
            // O Dash comanda a própria velocidade, então não zeramos se estiver dando dash
            if (!estaDandoDash) rb.velocity = Vector2.zero; 
            return;
        }

        rb.velocity = moveInput * moveSpeed; 

        // Aperte 'H' para causar 25 de dano no próprio jogador
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Player tomou dano para teste!");
            ReceberDano(25f);
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (estaMorto || estaAtacando || estaAtordoado) return;

        moveInput = context.ReadValue<Vector2>();   

        // --- LÓGICA DO DUPLO CLIQUE (DASH) ---
        if (context.started && moveInput.sqrMagnitude > 0)
        {
            // Verifica se a mesma direção foi apertada rapidamente
            if (Time.time - ultimoTempoAperto <= tempoDuploClique && Vector2.Distance(moveInput, ultimaDirecaoApertada) < 0.1f)
            {
                if (!estaDandoDash) StartCoroutine(RotinaDash(moveInput));
            }
            
            ultimoTempoAperto = Time.time;
            ultimaDirecaoApertada = moveInput;
        }

        // --- LÓGICA DE ANIMAÇÃO ---
        if (moveInput.sqrMagnitude > 0) 
        {
            animator.SetBool("isWalking", true);
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
        }
        else 
        {
            animator.SetBool("isWalking", false);
        }

        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);
    }

    public void Atacar(InputAction.CallbackContext context)
    {
        if (context.started && !estaAtacando && !estaMorto && !estaAtordoado && !estaDandoDash)
        {
            StartCoroutine(RotinaAtaque());
        }
    }

    public void ReceberDano(float dano)
    {
        if (estaMorto || estaAtordoado || estaDandoDash) return; // Dá imunidade enquanto usa o dash!
        
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
    private IEnumerator RotinaDash(Vector2 direcao)
    {
        estaDandoDash = true;
        animator.SetBool("isWalking", false);
        
        // Dá o impulso explosivo de velocidade
        rb.velocity = direcao * velocidadeDash;
        
        // Aqui você pode adicionar um rastro ou poeira depois
        yield return new WaitForSeconds(tempoDash);
        
        rb.velocity = Vector2.zero;
        estaDandoDash = false;
    }

    private IEnumerator RotinaAtaque()
    {
        estaAtacando = true;
        moveInput = Vector2.zero; 
        animator.SetBool("isWalking", false);
        
        animator.SetTrigger("Attack");
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
        yield return new WaitForSeconds(0.3f); 
        estaAtordoado = false;
    }

    private IEnumerator RotinaMorte()
    {
        estaMorto = true;
        moveInput = Vector2.zero;
        rb.isKinematic = true; 
        animator.SetTrigger("Death");
        yield return new WaitForSeconds(2.0f);
    }
}