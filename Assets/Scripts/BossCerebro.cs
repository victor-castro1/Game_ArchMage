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
    public float velocidadeDash = 20f; // Aumentei um pouco para dar mais impacto
    public float tempoDash = 0.2f; 
    public float tempoAtaque = 1.0f; 
    public float tempoRecuperacao = 1.5f; 
    private bool estaAtacando = false;
    private bool emCooldownAtaque = false; // 🚨 NOVA TRAVA PARA NÃO SPAMMAR DASH

    [Header("Efeito de Dano")]
    public Material materialFlash; 
    private Material materialOriginal;

    private Transform alvoJogador;
    private Rigidbody2D rb;
    private SpriteRenderer sr; 
    private Animator anim;
    
    private bool estaAtordoado = false; 
    private CinemachineImpulseSource tremor;
    private bool naFase2 = false; 

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
    }

    void FixedUpdate()
    {
        if (alvoJogador == null || estaAtordoado || estaInvocando || estaTeletransportando || estaAtacando)
        {
            // Se estiver no meio do ataque (dash), a rotina de ataque assume o controle da velocidade
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
                    RaycastHit2D hitParede = Physics2D.Raycast(transform.position, direcao, 1.5f, LayerMask.GetMask("Obstaculo"));
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
                    if (alvoJogador.position.x > transform.position.x) sr.flipX = false; 
                    else sr.flipX = true;
                }
            }
            else 
            {
                // --- FASE 2: Caça, Desvia e Ataca ---
                // Só caça se não estiver atirando E não estiver cansado do último golpe
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
                    rb.velocity = Vector2.zero; // Fica parado recuperando o fôlego
                }
            }
        }

        // FlipX do movimento normal
        if (direcao != Vector2.zero) 
        {
            if (direcao.x > 0) sr.flipX = true; 
            else if (direcao.x < 0) sr.flipX = false; 
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) ReceberDano(25f);
    }

    public void ReceberDano(float quantidadeDano)
    {
        if (estaAtordoado) return; 

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
        estaAtordoado = true; 
        rb.velocity = Vector2.zero;
        rb.isKinematic = true; 
        anim.SetTrigger("SofreuDano"); 
        yield return new WaitForSeconds(1.0f); 
        Destroy(gameObject); 
    }

    private IEnumerator RotinaAtaque()
    {
        estaAtacando = true;
        rb.velocity = Vector2.zero; 

        Vector2 posicaoAlvo = alvoJogador.position;
        Vector2 direcaoDash = (posicaoAlvo - (Vector2)transform.position).normalized;
        
        // 🚨 FIX: Ajusta o olhar dele ANTES de pular (não ataca mais de costas)
        if (direcaoDash.x > 0) sr.flipX = true; 
        else if (direcaoDash.x < 0) sr.flipX = false;

        // Animação de se preparar
        yield return new WaitForSeconds(0.4f); 

        // 🚨 FIX: Dash Explosivo usando Física (Mata o efeito flutuante)
        rb.velocity = direcaoDash * velocidadeDash;
        yield return new WaitForSeconds(tempoDash);
        
        // Freia bruscamente
        rb.velocity = Vector2.zero; 
        anim.SetTrigger("Atacar"); 
        
        yield return new WaitForSeconds(tempoAtaque); 

        // 🚨 FIX: Inicia o cooldown DEPOIS de soltar o controle do movimento
        estaAtacando = false; 
        emCooldownAtaque = true;
        yield return new WaitForSeconds(tempoRecuperacao); 
        emCooldownAtaque = false;
    }

    private IEnumerator RotinaFlash()
    {
        sr.material = materialFlash; 
        yield return new WaitForSeconds(0.1f); 
        sr.material = materialOriginal; 
    }

    private IEnumerator RotinaInvocacao()
    {
        while (true) 
        {
            yield return new WaitForSeconds(tempoEntreInvocacoes);
            int minionsVivos = GameObject.FindGameObjectsWithTag("Minion").Length;

            if (!naFase2 && !estaAtordoado && minionsVivos < limiteMaximoMinions)
            {
                estaInvocando = true;
                rb.velocity = Vector2.zero; 
                anim.SetTrigger("Invocando");
                yield return new WaitForSeconds(2.0f);

                if (prefabMinion != null)
                {
                    Instantiate(prefabMinion, transform.position + new Vector3(-2f, 0, 0), Quaternion.identity);
                    Instantiate(prefabMinion, transform.position + new Vector3(2f, 0, 0), Quaternion.identity);
                }

                yield return new WaitForSeconds(0.5f);
                estaInvocando = false;
            }
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

   private IEnumerator RotinaTeletransporte()
    {
        estaTeletransportando = true; 
        rb.velocity = Vector2.zero;
        sr.color = new Color(1, 1, 1, 0.5f); 
        yield return new WaitForSeconds(0.4f);
        
        Vector2 direcaoLonge = (transform.position - alvoJogador.position).normalized;
        transform.position = (Vector2)alvoJogador.position + (direcaoLonge * 8f);
        
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