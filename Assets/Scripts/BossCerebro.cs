using UnityEngine;
using System.Collections;
using Cinemachine;
using UnityEngine.UIElements;

public class BossCerebro : MonoBehaviour
{
    private bool estaTeletransportando = false;

    [Header("UI do Boss")]
    public UIDocument mainHudDocument; 
    private VisualElement bossPanel;
    private VisualElement bossHpBar;
    private float vidaMaximaBoss;
    private VisualElement filtroEscuro;
    
    [Header("Configurações de Movimento")]
    public float velocidade = 3.0f;
    public float raioDeteccao = 10f;
    public float distanciaMaximaFuga = 7.0f; 
    [Tooltip("Distância do raio de detecção de paredes. Aumente se o Boss for muito grande.")]
    public float distanciaRaycast = 3.5f; 

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
    public float tempoAtaque = 0.8f; 
    public float tempoRecuperacao = 1.2f; 
    private bool estaAtacando = false;
    private bool emCooldownAtaque = false; 

    [Header("Efeito de Dano")]
    public Material materialFlash;
    private Material materialOriginal;

    [Header("Debug")]
    [Tooltip("Tecla T causa dano no chefe (teste). MANTENHA DESLIGADO na apresentação.")]
    public bool modoDebug = false;

    private Transform alvoJogador;
    private Rigidbody2D rb;
    private SpriteRenderer sr; 
    private Animator anim;
    
    private bool estaAtordoado = false; 
    private bool estaMorto = false;
    private CinemachineImpulseSource tremor;
    private bool naFase2 = false; 

    private Vector3 escalaOriginal;

    private bool jogadorDetectado = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); 
        anim = GetComponent<Animator>();
        tremor = GetComponent<CinemachineImpulseSource>();

        materialOriginal = sr.material;

        GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jogadorObj != null) alvoJogador = jogadorObj.transform;

        //StartCoroutine(RotinaInvocacao());
       
        escalaOriginal = transform.localScale;

        vidaMaximaBoss = vidaTotal;

        if (mainHudDocument != null)
        {
            var root = mainHudDocument.rootVisualElement;
            bossPanel = root.Q<VisualElement>("boss-panel");
            bossHpBar = root.Q<VisualElement>("boss-hp-bar");
            filtroEscuro = root.Q<VisualElement>("filtro-escuro"); 
        }
    }

    void FixedUpdate()
    {
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
            // 🚨 Limpo: Apenas uma chamada para ligar o HUD e o Filtro
            if (bossPanel != null) bossPanel.style.display = DisplayStyle.Flex;
            if (filtroEscuro != null) filtroEscuro.style.display = DisplayStyle.Flex;

            if (!jogadorDetectado)
            {
                jogadorDetectado = true;
                if (tremor != null) tremor.GenerateImpulse();
                StartCoroutine(RotinaInvocacao());
            }

            if (!naFase2) 
            {
                // --- FASE 1: MANTÉM A DISTÂNCIA ---
                float margemTolerancia = 0.5f; 

                if (distancia < (distanciaMaximaFuga - margemTolerancia))
                {
                    direcao = (transform.position - alvoJogador.position).normalized; 
                    
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
        if (modoDebug && Input.GetKeyDown(KeyCode.T)) ReceberDano(25f);
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

        if (bossHpBar != null)
        {
            float porcentagem = Mathf.Clamp(vidaTotal / vidaMaximaBoss, 0f, 1f);
            bossHpBar.transform.scale = new Vector3(porcentagem, 1f, 1f);
        }

        // 🚨 AJUSTE: Gatilho da Fase 2 (50% da vida)
        if (!naFase2 && vidaTotal <= (vidaMaximaBoss / 2))
        {
            StartCoroutine(RotinaTransformacaoFase2());
        }
        
        if (vidaTotal <= 0) 
        {
            if (bossPanel != null) bossPanel.style.display = DisplayStyle.None;
            if (filtroEscuro != null) filtroEscuro.style.display = DisplayStyle.None; 
            StartCoroutine(RotinaMorte());
        }
        else
        {
            // 🚨 AJUSTE: Impede atordoamento se ele estiver no meio de um ataque
            if (!estaAtacando) 
            {
                StartCoroutine(RotinaAtordoamento());
            }
        }
    }

    private IEnumerator RotinaMorte()
    {
        estaMorto = true;
        estaAtordoado = true; 
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; 

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        anim.SetTrigger("SofreuDano");
        yield return new WaitForSeconds(1.0f);

        // 🚨 CHEFE DERROTADO = VITÓRIA DO JOGADOR
        GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jogadorObj != null) jogadorObj.GetComponent<MovimentoJogador>()?.MostrarVitoria();

        this.enabled = false;
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
        sr.color = new Color(1, 1, 1, 0.5f); 
        yield return new WaitForSeconds(0.4f);
        
        Vector2 direcaoLonge = (transform.position - alvoJogador.position).normalized;
        float distanciaDesejada = 7f;

        RaycastHit2D hit = Physics2D.Raycast(alvoJogador.position, direcaoLonge, distanciaDesejada, LayerMask.GetMask("Obstaculo"));

        if (hit.collider != null)
        {
            distanciaDesejada = hit.distance - 2.0f; 
            if (distanciaDesejada < 0) distanciaDesejada = 0;
        }

        Vector2 destinoTeleporte = (Vector2)alvoJogador.position + (direcaoLonge * distanciaDesejada);
        transform.position = destinoTeleporte;
        
        sr.color = new Color(1, 1, 1, 1f); 
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