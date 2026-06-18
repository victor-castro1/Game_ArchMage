using UnityEngine;
using System.Collections;

public class SlimeIA : MonoBehaviour
{
    [Header("Status do Slime")]
    public float vidaTotal = 30f;
    public float velocidade = 2.5f;

    [Header("Combate")]
    public float distanciaParaAtacar = 1.2f; 
    public float tempoEntreAtaques = 1.5f;   
    public float tempoAtordoado = 0.3f;      
    
    [Header("Efeito de Dano")]
    public Material materialFlash;
    private Material materialOriginal;

    [Header("Debug")]
    [Tooltip("Tecla Y causa dano em TODOS os slimes (teste). MANTENHA DESLIGADO na apresentação.")]
    public bool modoDebug = false;

    private Transform alvoJogador;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private bool estaAtacando = false;
    private bool estaAtordoado = false;
    private bool estaMorto = false;

    // 🚨 VARIÁVEL PARA GUARDAR O TAMANHO DO SLIME
    private Vector3 escalaOriginal;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        materialOriginal = sr.material;

        GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jogadorObj != null) alvoJogador = jogadorObj.transform;

        // Salva o tamanho original colocado no Inspector
        escalaOriginal = transform.localScale;
    }

    void FixedUpdate()
    {
        // Se morreu, corta a execução do script na hora
        if (!this.enabled || estaMorto) return;

        if (alvoJogador == null || estaAtordoado || estaAtacando)
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("Andando", false);
            return;
        }

        float distancia = Vector2.Distance(transform.position, alvoJogador.position);
        Vector2 direcao = (alvoJogador.position - transform.position).normalized;

        if (distancia <= distanciaParaAtacar)
        {
            StartCoroutine(RotinaAtaque());
        }
        else
        {
            Vector2 direcaoSegura = DesviarDeObstaculos(direcao);
            rb.MovePosition(rb.position + direcaoSegura * velocidade * Time.fixedDeltaTime);
            anim.SetBool("Andando", true);
        }

        // 🚨 O NOVO TRUQUE DO FLIP (Gira o corpo e a Hitbox)
        if (direcao != Vector2.zero)
        {
            if (direcao.x > 0) 
            {
                // Indo para a Direita: espelha (negativo)
                transform.localScale = new Vector3(-Mathf.Abs(escalaOriginal.x), escalaOriginal.y, escalaOriginal.z);
            }
            else if (direcao.x < 0) 
            {
                // Indo para a Esquerda: normal (positivo)
                transform.localScale = new Vector3(Mathf.Abs(escalaOriginal.x), escalaOriginal.y, escalaOriginal.z);
            }
        }
    }

    void Update()
    {
        if (modoDebug && Input.GetKeyDown(KeyCode.Y)) ReceberDano(10f);
    }

    public void ReceberDano(float quantidade)
    {
        if (estaMorto || estaAtordoado || !this.enabled) return;

        vidaTotal -= quantidade;

        // 🚨 TRAVA DE VIDA NEGATIVA
        if (vidaTotal < 0f) vidaTotal = 0f;

        StartCoroutine(RotinaFlash());

        if (vidaTotal <= 0)
        {
            StartCoroutine(RotinaMorte());
        }
        else
        {
            anim.SetTrigger("Hurt");
            StartCoroutine(RotinaAtordoamento());
        }
    }

    private Vector2 DesviarDeObstaculos(Vector2 direcaoAlvo)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direcaoAlvo, 1.5f, LayerMask.GetMask("Obstaculo"));

        if (hit.collider != null)
        {
            Vector2 desvioDireita = new Vector2(-direcaoAlvo.y, direcaoAlvo.x);
            RaycastHit2D hitDesvio = Physics2D.Raycast(transform.position, desvioDireita, 1.5f, LayerMask.GetMask("Obstaculo"));

            if (hitDesvio.collider == null) return desvioDireita; 

            return new Vector2(direcaoAlvo.y, -direcaoAlvo.x);
        }

        return direcaoAlvo; 
    }

    private IEnumerator RotinaAtaque()
    {
        estaAtacando = true;
        rb.velocity = Vector2.zero;
        anim.SetBool("Andando", false);

        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.5f);

        yield return new WaitForSeconds(tempoEntreAtaques);
        estaAtacando = false;
    }

    private IEnumerator RotinaFlash()
    {
        sr.material = materialFlash;
        sr.color = Color.red; // Tinta vermelha caso o Animator bloqueie o material
        yield return new WaitForSeconds(0.1f);
        sr.material = materialOriginal;
        sr.color = Color.white;
    }

    private IEnumerator RotinaAtordoamento()
    {
        estaAtordoado = true;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(tempoAtordoado);
        estaAtordoado = false;
    }

    private IEnumerator RotinaMorte()
    {
        estaMorto = true;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        // 🚨 ADICIONE ISSO: Procura o jogador e dá carga no especial
        GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jogadorObj != null)
        {
            MovimentoJogador scriptPlayer = jogadorObj.GetComponent<MovimentoJogador>();
            if (scriptPlayer != null)
            {
                // Aumenta o valor do especial
                scriptPlayer.especialAtual += 10f; 
                
                // Garante que não passe do máximo
                if (scriptPlayer.especialAtual > scriptPlayer.especialMaximo) 
                    scriptPlayer.especialAtual = scriptPlayer.especialMaximo;
                
                // Chama a atualização visual do HUD do jogador
                scriptPlayer.Invoke("AtualizarHUD", 0.1f);
            }
        }

        anim.SetTrigger("Death");
        yield return new WaitForSeconds(1.0f);
        this.enabled = false; 
        Destroy(gameObject); 
    }
}