using UnityEngine;
using System.Collections;

public class SlimeIA : MonoBehaviour
{
    [Header("Status do Slime")]
    public float vidaTotal = 30f;
    public float velocidade = 2.5f;

    [Header("Combate")]
    public float distanciaParaAtacar = 1.2f; // Quão perto ele precisa chegar para bater
    public float tempoEntreAtaques = 1.5f;   // Cooldown do ataque
    public float tempoAtordoado = 0.3f;      // Tempo que ele trava quando toma hit
    
    [Header("Efeito de Dano")]
    public Material materialFlash;
    private Material materialOriginal;

    private Transform alvoJogador;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private bool estaAtacando = false;
    private bool estaAtordoado = false;
    private bool estaMorto = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        materialOriginal = sr.material;

        // Já nasce procurando o Player na cena
        GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jogadorObj != null) alvoJogador = jogadorObj.transform;
    }

    void FixedUpdate()
    {
        // 🚨 ADICIONAMOS O estaAtacando AQUI
        if (alvoJogador == null || estaMorto || estaAtordoado || estaAtacando)
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("Andando", false);
            return;
        }

        float distancia = Vector2.Distance(transform.position, alvoJogador.position);
        Vector2 direcao = (alvoJogador.position - transform.position).normalized;

        if (distancia <= distanciaParaAtacar)
        {
            // Chegou perto o suficiente: Para e ataca
            StartCoroutine(RotinaAtaque());
        }
        else
        {
            // Longe: Persegue o jogador
            Vector2 direcaoSegura = DesviarDeObstaculos(direcao);
            rb.MovePosition(rb.position + direcaoSegura * velocidade * Time.fixedDeltaTime);
            anim.SetBool("Andando", true);
        }

        // --- O TRUQUE DO FLIP ---
        if (direcao != Vector2.zero)
        {
            if (direcao.x > 0) sr.flipX = true;
            else if (direcao.x < 0) sr.flipX = false;
        }
    }

    // Botão de teste para você debugar o dano
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y)) ReceberDano(10f); // Aperte Y para testar
    }

    public void ReceberDano(float quantidade)
    {
        if (estaMorto || estaAtordoado) return;

        vidaTotal -= quantidade;

        // O clássico flash de dano que configuramos
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
        // Atira um raio 1.5 metros para frente buscando a Layer "Obstaculo"
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direcaoAlvo, 1.5f, LayerMask.GetMask("Obstaculo"));

        if (hit.collider != null)
        {
            // Tem parede! Tenta desviar pela direita (Calcula a perpendicular)
            Vector2 desvioDireita = new Vector2(-direcaoAlvo.y, direcaoAlvo.x);
            RaycastHit2D hitDesvio = Physics2D.Raycast(transform.position, desvioDireita, 1.5f, LayerMask.GetMask("Obstaculo"));

            if (hitDesvio.collider == null) return desvioDireita; // Direita livre!

            // Se direita tá bloqueada, vai pela esquerda
            return new Vector2(direcaoAlvo.y, -direcaoAlvo.x);
        }

        return direcaoAlvo; // Caminho totalmente livre
    }

    private IEnumerator RotinaAtaque()
    {
        estaAtacando = true;
        rb.velocity = Vector2.zero;
        anim.SetBool("Andando", false);

        anim.SetTrigger("Attack");

        // Tempo para a animação do ataque rolar (Ajuste conforme sua animação)
        yield return new WaitForSeconds(0.5f);

        // AQUI ENTRARÁ A LÓGICA DE CAUSAR DANO NO PLAYER NO FUTURO

        // Espera o cooldown antes de voltar a andar
        yield return new WaitForSeconds(tempoEntreAtaques);
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
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(tempoAtordoado);
        estaAtordoado = false;
    }

    private IEnumerator RotinaMorte()
    {
        estaMorto = true;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true; // Cai duro no chão

        anim.SetTrigger("Death");

        // Tempo certinho daquela sua animação de derretendo/ossos quebrando
        yield return new WaitForSeconds(1.0f);

        Destroy(gameObject);
    }
}