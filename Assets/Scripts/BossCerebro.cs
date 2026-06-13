using UnityEngine;
using System.Collections;
using Cinemachine;

public class BossCerebro : MonoBehaviour
{

    private bool estaTeletransportando = false;
    
    [Header("Configurações de Movimento")]
    public float velocidade = 3.0f;
    public float raioDeteccao = 10f;
    public float distanciaMaximaFuga = 7.0f; // O quão longe ele aceita ficar do player

    [Header("Status do Boss")]
    public float vidaTotal = 100f;
    public float tempoAtordoado = 0.4f;

    [Header("Configurações de Invocação")]
    public GameObject prefabMinion; 
    public float tempoEntreInvocacoes = 6f; 
    public int limiteMaximoMinions = 4; // 🚨 A NOVA TRAVA
    private bool estaInvocando = false;

    [Header("Configurações de Ataque (Fase 2)")]
    public float distanciaParaIniciarAtaque = 6.0f; 
    public float velocidadeDash = 15f; 
    public float tempoDash = 0.2f; 
    public float tempoAtaque = 1.0f; 
    public float tempoRecuperacao = 1.5f; 
    private bool estaAtacando = false;

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
        // Trava total se estiver atordoado, invocando ou teleportando
        if (alvoJogador == null || estaAtordoado || estaInvocando || estaTeletransportando)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float distancia = Vector2.Distance(transform.position, alvoJogador.position);
        Vector2 direcao = Vector2.zero;

        if (distancia <= raioDeteccao)
        {
            if (!naFase2) 
            {
                // --- FASE 1: Foge até a distância máxima ---
                if (distancia < distanciaMaximaFuga)
                {
                    direcao = (transform.position - alvoJogador.position).normalized; 
                    
                    // 🚨 Antena: Vê se tem parede atrás dele enquanto foge
                    RaycastHit2D hitParede = Physics2D.Raycast(transform.position, direcao, 1.5f, LayerMask.GetMask("Obstaculo"));
                    
                    if (hitParede.collider != null)
                    {
                        StartCoroutine(RotinaTeletransporte()); // Encurralado! Teleporta!
                    }
                    else
                    {
                        rb.MovePosition(rb.position + direcao * velocidade * Time.fixedDeltaTime);
                    }
                }
                else
                {
                    // Alcançou a distância segura! Fica parado e encara o jogador
                    rb.velocity = Vector2.zero;
                    direcao = Vector2.zero; 
                    
                    if (alvoJogador.position.x > transform.position.x) sr.flipX = false; 
                    else sr.flipX = true;
                }
            }
            else 
            {
                // --- 🚨 FASE 2: Caça, Desvia e Ataca (Estava faltando!) ---
                if (!estaAtacando) 
                {
                    if (distancia <= distanciaParaIniciarAtaque)
                    {
                        StartCoroutine(RotinaAtaque());
                    }
                    else 
                    {
                        direcao = (alvoJogador.position - transform.position).normalized; 
                        
                        // Chama a inteligência de desvio!
                        Vector2 direcaoSegura = DesviarDeObstaculos(direcao);
                        rb.MovePosition(rb.position + direcaoSegura * velocidade * Time.fixedDeltaTime);
                    }
                }
            }
        }

        // FlipX
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
        
        // Trigger de feedback visual de dano
        StartCoroutine(RotinaFlash());

        if (vidaTotal <= 0) 
        {
            // 🚨 AGORA ELE CHAMA A ROTINA DE MORTE
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
        // 1. Congela o Boss
        estaAtordoado = true; 
        rb.velocity = Vector2.zero;
        rb.isKinematic = true; // Impede que forças externas empurrem ele enquanto morre
        
        // 2. Toca a animação de dano final (o "Hurt" que reaproveitamos)
        anim.SetTrigger("SofreuDano"); 

        // 3. Opcional: Feedback final (Flash Branco ou redução de alpha)
        // Se quiser que ele desapareça piscando, pode adicionar aqui
        
        // 4. Espera o tempo da animação/feedback
        yield return new WaitForSeconds(1.0f); 
        
        // 5. Remove da cena
        Destroy(gameObject); 
    }
    private IEnumerator RotinaAtaque()
    {
        estaAtacando = true;
        rb.velocity = Vector2.zero; 

        Vector2 posicaoAlvo = alvoJogador.position;
        Vector2 direcaoDash = (posicaoAlvo - (Vector2)transform.position).normalized;
        yield return new WaitForSeconds(0.4f); 

        float timer = 0f;
        while (timer < tempoDash)
        {
            rb.MovePosition(rb.position + direcaoDash * velocidadeDash * Time.fixedDeltaTime);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.velocity = Vector2.zero; 
        anim.SetTrigger("Atacar"); 
        
        yield return new WaitForSeconds(tempoAtaque); 
        yield return new WaitForSeconds(tempoRecuperacao); 

        estaAtacando = false; 
    }

    private IEnumerator RotinaFlash()
    {
        // Troca o material atual pelo material de flash
        sr.material = materialFlash; 
        
        yield return new WaitForSeconds(0.1f); // Duração
        
        // Volta para o material original
        sr.material = materialOriginal; 
    }
    private IEnumerator RotinaInvocacao()
    {
        while (true) 
        {
            yield return new WaitForSeconds(tempoEntreInvocacoes);

            // 🚨 CONTA QUANTOS MINIONS EXISTEM VIVOS
            int minionsVivos = GameObject.FindGameObjectsWithTag("Minion").Length;

            // Só invoca se não bateu o limite
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

   private IEnumerator RotinaTeletransporte()
    {
        estaTeletransportando = true; // 🚨 Trava o Boss
        rb.velocity = Vector2.zero;
        
        sr.color = new Color(1, 1, 1, 0.5f); // Fica transparente
        
        yield return new WaitForSeconds(0.4f);
        
        // Pula por cima do jogador para o lado oposto (8 metros)
        Vector2 direcaoLonge = (transform.position - alvoJogador.position).normalized;
        transform.position = (Vector2)alvoJogador.position + (direcaoLonge * 8f);
        
        sr.color = new Color(1, 1, 1, 1f); // Volta ao normal
        yield return new WaitForSeconds(0.2f);
        
        estaTeletransportando = false; // 🚨 Libera o Boss
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