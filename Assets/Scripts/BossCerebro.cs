using UnityEngine;
using System.Collections;
using Cinemachine;

public class BossCerebro : MonoBehaviour
{
    private bool estaTeletransportando = false;
    
    [Header("Configurações de Movimento")]
    public float velocidade = 3.0f;
    public float raioDeteccao = 10f;
    public float distanciaMaximaFuga = 7.0f; 
    [Tooltip("Distância do raio de detecção de paredes. Aumente se o Boss for muito grande.")]
    public float distanciaRaycast = 3.5f; // 🚨 AJUSTE PARA O TAMANHO DELE (Escala 3)

    [Header("Status do Boss")]
    public float vidaTotal = 100f;
    public float tempoAtordoado = 0.4f;

    [Header("Configurações de Invocação")]
    public GameObject prefabMinion; 
    public float tempoEntreInvocacoes = 6f; 
    public int limiteMaximoMinions = 4; 
    private bool estaInvocando = false;

    [Header("Configurações de Ataque (Fase 2)")]
    public float distanciaParaIniciarAtaque = 6.0f; 
    public float velocidadeDash = 20f; 
    public float tempoDash = 0.2f; 
    public float tempoAtaque = 0.8f; // Reduzido um pouco para dar agilidade
    public float tempoRecuperacao = 1.2f; // Reduzido um pouco para ser mais responsivo
    private bool estaAtacando = false;
    private bool emCooldownAtaque = false; 

    [Header("Efeito de Dano")]
    public Material materialFlash; 
    private Material materialOriginal;

    private Transform alvoJogador;
    private Rigidbody2D rb;
    private SpriteRenderer sr; 
    private Animator anim;
    
    private bool estaAtordoado = false; 
    private bool estaMorto = false;
    private CinemachineImpulseSource tremor;
    private bool naFase2 = false; 

    private Vector3 escalaOriginal;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); 
        anim = GetComponent<Animator>();
        tremor = GetComponent<CinemachineImpulseSource>();

        materialOriginal = sr.material;

        GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jogadorObj != null) alvoJogador = jogadorObj.transform;

        StartCoroutine(RotinaInvocacao());

        escalaOriginal = transform.localScale;
    }

    void FixedUpdate()
    {
       // 🚨 FIX: Se estiver morto ou o script desligado, ignora a física imediatamente
        if (estaMorto || !this.enabled) return;

        if (alvoJogador == null || estaAtordoado || estaInvocando || estaTeletransportando || estaAtacando)
        {
            if (!estaAtacando) rb.velocity = Vector2.zero;
            return;
        }

        float distancia = Vector2.Distance(transform.position, alvoJogador.position);
        Vector2 direcao = Vector2.zero;

        if (distancia <= raioDeteccao)
        {
            if (!naFase2) 
            {
                // --- FASE 1: MANTÉM A DISTÂNCIA ---
                float margemTolerancia = 0.5f; 

                if (distancia < (distanciaMaximaFuga - margemTolerancia))
                {
                    direcao = (transform.position - alvoJogador.position).normalized; 
                    
                    // 🚨 FIX: Raio estendido usando distanciaRaycast
                    RaycastHit2D hitParede = Physics2D.Raycast(transform.position, direcao, distanciaRaycast, LayerMask.GetMask("Obstaculo"));
                    if (hitParede.collider != null)
                    {
                        StartCoroutine(RotinaTeletransporte());
                    }
                    else
                    {
                        rb.MovePosition(rb.position + direcao * velocidade * Time.fixedDeltaTime);
                    }
                }
                else if (distancia > (distanciaMaximaFuga + margemTolerancia))
                {
                    direcao = (alvoJogador.position - transform.position).normalized; 
                    rb.MovePosition(rb.position + direcao * velocidade * Time.fixedDeltaTime);
                }
                else
                {
                    rb.velocity = Vector2.zero;
                    direcao = Vector2.zero; 
                    LookAtPlayer();
                }
            }
            else 
            {
                // --- FASE 2: Caça, Desvia e Ataca ---
                if (!emCooldownAtaque) 
                {
                    if (distancia <= distanciaParaIniciarAtaque)
                    {
                        StartCoroutine(RotinaAtaque());
                    }
                    else 
                    {
                        direcao = (alvoJogador.position - transform.position).normalized; 
                        Vector2 direcaoSegura = DesviarDeObstaculos(direcao);
                        rb.MovePosition(rb.position + direcaoSegura * velocidade * Time.fixedDeltaTime);
                    }
                }
                else
                {
                    // 🚨 FIX MAIS RESPONSIVO: Em vez de congelar, ele persegue mais devagar (40% da velocidade) durante o cooldown!
                    direcao = (alvoJogador.position - transform.position).normalized; 
                    Vector2 direcaoSegura = DesviarDeObstaculos(direcao);
                    rb.MovePosition(rb.position + direcaoSegura * (velocidade * 0.4f) * Time.fixedDeltaTime);
                }
            }
        }

        // Sistema de Flip
        if (direcao != Vector2.zero) 
        {
            if (direcao.x > 0) 
                transform.localScale = new Vector3(-Mathf.Abs(escalaOriginal.x), escalaOriginal.y, escalaOriginal.z); 
            else if (direcao.x < 0) 
                transform.localScale = new Vector3(Mathf.Abs(escalaOriginal.x), escalaOriginal.y, escalaOriginal.z); 
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) ReceberDano(25f);
    }

    private void LookAtPlayer()
    {
        if (alvoJogador == null) return;
        if (alvoJogador.position.x > transform.position.x) 
            transform.localScale = new Vector3(-Mathf.Abs(escalaOriginal.x), escalaOriginal.y, escalaOriginal.z); 
        else 
            transform.localScale = new Vector3(Mathf.Abs(escalaOriginal.x), escalaOriginal.y, escalaOriginal.z);
    }

    public void ReceberDano(float quantidadeDano)
    {
        if (estaAtordoado || !this.enabled) return; 

        vidaTotal -= quantidadeDano;
        StartCoroutine(RotinaFlash());

        if (vidaTotal <= 0) 
        {
            StartCoroutine(RotinaMorte());
        }
        else if (vidaTotal <= 50f && !naFase2)
        {
            StartCoroutine(RotinaTransformacaoFase2());
        }
        else
        {
            StartCoroutine(RotinaAtordoamento());
        }
    }

    private IEnumerator RotinaMorte()
    {
        estaMorto = true;
        estaAtordoado = true; 
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; // 🚨 FIX: Congela o corpo fisicamente no mapa

        // 🚨 FIX: Desativa todos os colisores (incluindo os filhos de dano) para o player passar por cima do cadáver
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        anim.SetTrigger("SofreuDano"); 
        yield return new WaitForSeconds(1.0f); 

        this.enabled = false; // 🚨 FIX: Desativa o script do cérebro, mas preserva o SpriteRenderer na cena!
    }

    private IEnumerator RotinaAtaque()
    {
        estaAtacando = true;
        rb.velocity = Vector2.zero; 

        Vector2 posicaoAlvo = alvoJogador.position;
        Vector2 direcaoDash = (posicaoAlvo - (Vector2)transform.position).normalized;
        
        if (direcaoDash.x > 0) transform.localScale = new Vector3(-Mathf.Abs(escalaOriginal.x), escalaOriginal.y, escalaOriginal.z); 
        else if (direcaoDash.x < 0) transform.localScale = new Vector3(Mathf.Abs(escalaOriginal.x), escalaOriginal.y, escalaOriginal.z);

        yield return new WaitForSeconds(0.3f); 

        rb.velocity = direcaoDash * velocidadeDash;
        yield return new WaitForSeconds(tempoDash);
        
        rb.velocity = Vector2.zero; 
        anim.SetTrigger("Atacar"); 
        
        yield return new WaitForSeconds(tempoAtaque); 

        estaAtacando = false; 
        emCooldownAtaque = true;
        yield return new WaitForSeconds(tempoRecuperacao); 
        emCooldownAtaque = false;
    }

    private IEnumerator RotinaFlash()
    {
        sr.material = materialFlash; 
        sr.color = Color.red; 
        yield return new WaitForSeconds(0.1f); 
        sr.material = materialOriginal; 
        sr.color = Color.white; 
    }

    private IEnumerator RotinaInvocacao()
    {
        while (true) 
        {
            yield return new WaitForSeconds(tempoEntreInvocacoes);
            if (!this.enabled) break;

            int minionsVivos = GameObject.FindGameObjectsWithTag("Minion").Length;

            if (!naFase2 && !estaAtordoado && minionsVivos < limiteMaximoMinions)
            {
                estaInvocando = true;
                rb.velocity = Vector2.zero; 
                anim.SetTrigger("Invocando");
                yield return new WaitForSeconds(2.0f);

                if (prefabMinion != null)
                {
                    Vector3 posEsquerda = transform.position + new Vector3(-2.5f, 0, 0);
                    Vector3 posDireita = transform.position + new Vector3(2.5f, 0, 0);

                    // 🚨 FIX ANTIVAZAMENTO: Se a parede do castelo estiver no ponto, invoca no pé do boss
                    if (Physics2D.OverlapCircle(posEsquerda, 0.6f, LayerMask.GetMask("Obstaculo"))) posEsquerda = transform.position;
                    if (Physics2D.OverlapCircle(posDireita, 0.6f, LayerMask.GetMask("Obstaculo"))) posDireita = transform.position;

                    Instantiate(prefabMinion, posEsquerda, Quaternion.identity);
                    Instantiate(prefabMinion, posDireita, Quaternion.identity);
                }

                yield return new WaitForSeconds(0.5f);
                estaInvocando = false;
            }
        }
    }
    
    private Vector2 DesviarDeObstaculos(Vector2 direcaoAlvo)
    {
        // 🚨 FIX: Raio estendido usando distanciaRaycast
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direcaoAlvo, distanciaRaycast, LayerMask.GetMask("Obstaculo"));
        if (hit.collider != null)
        {
            Vector2 desvioDireita = new Vector2(-direcaoAlvo.y, direcaoAlvo.x); 
            RaycastHit2D hitDesvio = Physics2D.Raycast(transform.position, desvioDireita, distanciaRaycast, LayerMask.GetMask("Obstaculo"));
            if (hitDesvio.collider == null) return desvioDireita; 
            return new Vector2(direcaoAlvo.y, -direcaoAlvo.x);
        }
        return direcaoAlvo; 
    }

    private IEnumerator RotinaTeletransporte()
    {
        estaTeletransportando = true; 
        rb.velocity = Vector2.zero;
        sr.color = new Color(1, 1, 1, 0.5f); // Fica meio transparente
        yield return new WaitForSeconds(0.4f);
        
        // Calcula a direção (fugindo do jogador) e a distância que ele quer ir
        Vector2 direcaoLonge = (transform.position - alvoJogador.position).normalized;
        float distanciaDesejada = 7f;

        // 🚨 O NOVO CÁLCULO (À prova de vazamento do mapa)
        // Atira um laser do jogador até o ponto de teleporte.
        RaycastHit2D hit = Physics2D.Raycast(alvoJogador.position, direcaoLonge, distanciaDesejada, LayerMask.GetMask("Obstaculo"));

        if (hit.collider != null)
        {
            // Se o laser bateu na parede, ele encurta o teleporte para parar 2 metros ANTES da parede
            // (2 metros é uma margem de segurança boa por causa do tamanho gigante dele)
            distanciaDesejada = hit.distance - 2.0f; 
            
            // Se o jogador estiver tão imprensado na parede que a distância ficar negativa, ele não teleporta pra longe
            if (distanciaDesejada < 0) distanciaDesejada = 0;
        }

        // Aplica o teleporte seguro
        Vector2 destinoTeleporte = (Vector2)alvoJogador.position + (direcaoLonge * distanciaDesejada);
        transform.position = destinoTeleporte;
        
        sr.color = new Color(1, 1, 1, 1f); // Volta ao normal
        yield return new WaitForSeconds(0.2f);
        estaTeletransportando = false; 
    }

    private IEnumerator RotinaTransformacaoFase2()
    {
        naFase2 = true; 
        estaAtordoado = true; 
        rb.velocity = Vector2.zero; 

        anim.SetTrigger("GritoFase2");
        anim.SetBool("Fase2", true);
        if (tremor != null) tremor.GenerateImpulse();

        yield return new WaitForSeconds(2.5f); 
        
        velocidade = 4.5f; 
        estaAtordoado = false; 
    }

    private IEnumerator RotinaAtordoamento()
    {
        estaAtordoado = true;
        rb.velocity = Vector2.zero; 
        yield return new WaitForSeconds(tempoAtordoado); 
        estaAtordoado = false; 
    }
}