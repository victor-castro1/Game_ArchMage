using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class MovimentoJogador : MonoBehaviour
{
    [Header("HUD (UI Toolkit)")]
    public UIDocument hudDocument;
    private VisualElement barraVida;
    private VisualElement barraFolego;
    private VisualElement barraEspecial;

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    [Header("Dash (Fôlego Azul)")]
    public float velocidadeDash = 15f;
    public float tempoDash = 0.2f;
    public float tempoDuploClique = 0.3f; 
    public float folegoMaximo = 100f;
    public float folegoAtual = 100f;
    public float custoDash = 30f; // Gasta 30% da barra por dash
    public float regeneracaoFolego = 20f; // Recupera 20% por segundo
    private bool estaDandoDash = false;
    private float ultimoTempoAperto;
    private Vector2 ultimaDirecaoApertada;

    [Header("Especial (Barra Verde)")]
    public float especialMaximo = 100f;
    public float especialAtual = 0f;
    public float ganhoPorAcerto = 20f; // 5 acertos enchem a barra inteira
    public float danoDoEspecial = 50f; // O especial arranca MUITA vida

    [Header("Status do Jogador")]
    public float vidaTotal = 100f;
    private float vidaMaxima = 100f;
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

        // 🚨 CONECTA AS BARRAS DA INTERFACE
        if (hudDocument != null)
        {
            var root = hudDocument.rootVisualElement;
            barraVida = root.Q<VisualElement>("hp-bar");
            barraFolego = root.Q<VisualElement>("mp-bar");
            barraEspecial = root.Q<VisualElement>("green-bar");
            AtualizarHUD();
        }
    }

    void Update()
    {
        if (estaMorto || estaAtacando || estaAtordoado || estaDandoDash)
        {
            if (!estaDandoDash) rb.velocity = Vector2.zero; 
            return;
        }

        rb.velocity = moveInput * moveSpeed; 

        // 🚨 REGENERAÇÃO DE FÔLEGO AUTOMÁTICA
        if (folegoAtual < folegoMaximo)
        {
            folegoAtual += regeneracaoFolego * Time.deltaTime;
            if (folegoAtual > folegoMaximo) folegoAtual = folegoMaximo;
            AtualizarHUD(); // Mantém a barrinha azul crescendo fluidamente
        }

        // Teste de Dano
        if (Input.GetKeyDown(KeyCode.H)) ReceberDano(25f);

        if (Input.GetKeyDown(KeyCode.P))
        {
            especialAtual = especialMaximo;
            AtualizarHUD();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (especialAtual < especialMaximo)
            {
                Debug.Log("Bloqueado: Falta energia na barra verde!");
            }
            else if (moveInput.sqrMagnitude == 0)
            {
                Debug.Log("Bloqueado: Você precisa estar andando para disparar o Dash!");
            }
            else
            {
                Debug.Log("ESPECIAL ATIVADO!");
                StartCoroutine(RotinaAtaqueEspecial(moveInput));
            }
        }
        
        // 🚨 BOTÃO TEMPORÁRIO PARA O ESPECIAL (Até você mapear no Input System)
        if (Input.GetKeyDown(KeyCode.Space) && especialAtual >= especialMaximo && moveInput.sqrMagnitude > 0)
        {
            StartCoroutine(RotinaAtaqueEspecial(moveInput));
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (estaMorto || estaAtacando || estaAtordoado) return;

        moveInput = context.ReadValue<Vector2>();   

        if (context.started && moveInput.sqrMagnitude > 0)
        {
            if (Time.time - ultimoTempoAperto <= tempoDuploClique && Vector2.Distance(moveInput, ultimaDirecaoApertada) < 0.1f)
            {
                // 🚨 SÓ DÁ DASH SE TIVER FÔLEGO (AZUL)
                if (!estaDandoDash && folegoAtual >= custoDash) 
                {
                    folegoAtual -= custoDash;
                    StartCoroutine(RotinaDash(moveInput));
                }
            }
            
            ultimoTempoAperto = Time.time;
            ultimaDirecaoApertada = moveInput;
        }

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
        if (estaMorto || estaAtordoado || estaDandoDash) return; 
        
        vidaTotal -= dano;

        // 🚨 A TRAVA: Se a vida cair abaixo de zero, crava ela no zero absoluto.
        if (vidaTotal < 0f) 
        {
            vidaTotal = 0f;
        }

        AtualizarHUD(); // Agora a barra nunca vai calcular uma escala negativa

        StartCoroutine(RotinaFlash());
        
        if (vidaTotal <= 0) // O <= 0 continua funcionando perfeitamente
        {
            StartCoroutine(RotinaMorte());
        }
        else
        {
            animator.SetTrigger("Hurt");
            StartCoroutine(RotinaAtordoamento());
        }
    }

    // 🚨 MÉTODO QUE A FOICE CHAMA QUANDO ACERTA
    public void GanharEspecial()
    {
        if (especialAtual < especialMaximo)
        {
            especialAtual += ganhoPorAcerto;
            if (especialAtual > especialMaximo) especialAtual = especialMaximo;
            AtualizarHUD(); // 🚨 ATUALIZA BARRA VERDE
        }
    }

    // 🚨 CONTROLA O VISUAL DAS BARRAS
    private void AtualizarHUD()
    {
        // Usa transform.scale em vez de style.width. 
        // O Vector3 funciona assim: (escalaX, escalaY, escalaZ). O 1f mantém o tamanho original.
        if (barraVida != null) barraVida.transform.scale = new Vector3(vidaTotal / vidaMaxima, 1f, 1f);
        if (barraFolego != null) barraFolego.transform.scale = new Vector3(folegoAtual / folegoMaximo, 1f, 1f);
        if (barraEspecial != null) barraEspecial.transform.scale = new Vector3(especialAtual / especialMaximo, 1f, 1f);
    }

    // --- CORROTINAS ---
    private IEnumerator RotinaDash(Vector2 direcao)
    {
        estaDandoDash = true;
        animator.SetBool("isWalking", false);
        AtualizarHUD(); 
        
        // 🚨 FICA INTANGÍVEL: Ignora colisão física com a Layer "Inimigo"
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Inimigo"), true);

        rb.velocity = direcao * velocidadeDash;
        yield return new WaitForSeconds(tempoDash);
        
        rb.velocity = Vector2.zero;
        estaDandoDash = false;

        // 🚨 VOLTA AO NORMAL: Reativa a colisão
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Inimigo"), false);
    }

    private IEnumerator RotinaAtaqueEspecial(Vector2 direcao)
    {
        estaDandoDash = true; 
        especialAtual = 0; 
        AtualizarHUD();

        animator.SetTrigger("Attack"); 
        sr.color = Color.cyan; 

        // 🚨 FICA INTANGÍVEL TAMBÉM NO ESPECIAL
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Inimigo"), true);

        rb.velocity = direcao * (velocidadeDash * 1.5f); 

        float timer = 0f;
        while(timer < tempoDash * 1.2f)
        {
            Collider2D[] monstrosAtropelados = Physics2D.OverlapCircleAll(transform.position, 1.5f);
            foreach (Collider2D col in monstrosAtropelados)
            {
                if (col.CompareTag("Boss")) col.GetComponentInParent<BossCerebro>()?.ReceberDano(danoDoEspecial);
                if (col.CompareTag("Minion")) col.GetComponentInParent<SlimeIA>()?.ReceberDano(danoDoEspecial);
            }

            timer += Time.deltaTime;
            yield return null; 
        }

        rb.velocity = Vector2.zero;
        sr.color = Color.white; 
        estaDandoDash = false;

        // 🚨 VOLTA AO NORMAL
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Inimigo"), false);
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