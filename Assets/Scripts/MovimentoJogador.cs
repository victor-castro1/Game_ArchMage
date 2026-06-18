using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MovimentoJogador : MonoBehaviour
{
    [Header("HUD (UI Toolkit)")]
    public UIDocument hudDocument;
    private VisualElement barraVida;
    private VisualElement barraFolego;
    private VisualElement barraEspecial;

    // Tela de fim de jogo (vitória/derrota/fase concluída)
    private VisualElement telaFim;
    private Label labelResultado;
    private Button botaoContinuar;
    private Button botaoReiniciar;
    private Button botaoMenu;
    private string cenaProxima;
    private bool jogoTerminou = false;

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
    public float regeneracaoFolego = 10f; // Recupera 20% por segundo
    private bool estaDandoDash = false;
    private float ultimoTempoAperto;
    private Vector2 ultimaDirecaoApertada;

    [Header("Especial (Barra Verde)")]
    public float especialMaximo = 100f;
    public float especialAtual = 0f;
    public float ganhoPorAcerto = 20f; // 5 acertos enchem a barra inteira
    public float danoDoEspecial = 50f; // O especial arranca MUITA vida

    [Header("Magia à Distância (botão direito do mouse)")]
    public float danoMagia = 20f;
    public float velocidadeMagia = 12f;
    public float tempoVidaMagia = 2f;
    public float cooldownMagia = 0.4f;
    public Color corMagia = new Color(0.4f, 0.8f, 1f);
    [Tooltip("Opcional: sprite do projétil. Se vazio, usa um círculo gerado em runtime.")]
    public Sprite spriteProjetil;
    private float proximoTiroMagia = 0f;
    private Vector2 direcaoOlhar = Vector2.down;
    private static Sprite spriteCirculoCache;

    // Eventos para o tutorial (TutorialFase1 escuta para avançar os passos)
    public System.Action AoMover, AoAtacar, AoLancarMagia, AoDarDash, AoUsarEspecial;

    [Header("Status do Jogador")]
    public float vidaTotal = 100f;
    private float vidaMaxima; // Retirado o valor fixo daqui
    private bool estaMorto = false;
    private bool estaAtacando = false;
    private bool estaAtordoado = false;

    [Header("Efeito de Dano")]
    public Material materialFlash;
    private Material materialOriginal;
    private SpriteRenderer sr;

    [Header("Game Feel (Juice)")]
    [Tooltip("Empurrão que o jogador leva ao ser atingido.")]
    public float forcaKnockback = 6f;
    [Tooltip("Duração do congelamento de tela (hit-stop) ao acertar um inimigo.")]
    public float duracaoHitStop = 0.05f;
    [Tooltip("Por quanto tempo o jogador desliza ao ser empurrado.")]
    public float duracaoKnockback = 0.15f;
    private bool emHitStop = false;
    private bool estaSofrendoKnockback = false;

    [Header("Debug")]
    [Tooltip("Liga as teclas de teste (H = dano, P = enche especial). MANTENHA DESLIGADO na apresentação.")]
    public bool modoDebug = false;

    void Start()
    {
        // Rede de segurança: garante que o jogo não comece congelado (timeScale persiste entre cenas)
        Time.timeScale = 1f;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        materialOriginal = sr.material;

        // 🚨 CORREÇÃO CRÍTICA: Garante que a vida máxima seja o que você colocou no Inspector!
        vidaMaxima = vidaTotal;

        // 🚨 CONECTA AS BARRAS DA INTERFACE
        if (hudDocument != null)
        {
            var root = hudDocument.rootVisualElement;
            barraVida = root.Q<VisualElement>("hp-bar");
            barraFolego = root.Q<VisualElement>("mp-bar");
            barraEspecial = root.Q<VisualElement>("green-bar");

            // 🚨 CONECTA A TELA DE FIM DE JOGO (vitória/derrota)
            telaFim = root.Q<VisualElement>("tela-fim");
            labelResultado = root.Q<Label>("lbl-resultado");
            botaoContinuar = root.Q<Button>("btn-continuar");
            botaoReiniciar = root.Q<Button>("btn-reiniciar");
            botaoMenu = root.Q<Button>("btn-menu");
            if (botaoContinuar != null) botaoContinuar.clicked += Continuar;
            if (botaoReiniciar != null) botaoReiniciar.clicked += Reiniciar;
            if (botaoMenu != null) botaoMenu.clicked += VoltarAoMenu;
            if (botaoContinuar != null) botaoContinuar.style.display = DisplayStyle.None;
            if (telaFim != null) telaFim.style.display = DisplayStyle.None;

            AtualizarHUD();
        }
    }

    void Update()
    {
        if (estaMorto || estaAtacando || estaAtordoado || estaDandoDash)
        {
            // Não zera a velocidade durante dash ou knockback (senão o empurrão não aparece)
            if (!estaDandoDash && !estaSofrendoKnockback) rb.velocity = Vector2.zero;
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

        // 🚨 TECLAS DE TESTE (só funcionam com modoDebug ligado no Inspector)
        if (modoDebug)
        {
            if (Input.GetKeyDown(KeyCode.H)) ReceberDano(25f);

            if (Input.GetKeyDown(KeyCode.P))
            {
                especialAtual = especialMaximo;
                AtualizarHUD();
            }
        }

        // ESPECIAL: dispara com Espaço quando a barra verde está cheia e o jogador está andando
        if (Input.GetKeyDown(KeyCode.Space) && especialAtual >= especialMaximo && moveInput.sqrMagnitude > 0)
        {
            StartCoroutine(RotinaAtaqueEspecial(moveInput));
        }

        // MAGIA À DISTÂNCIA: botão direito do mouse (com cooldown)
        if (Input.GetMouseButtonDown(1) && Time.time >= proximoTiroMagia)
        {
            LancarMagia();
            proximoTiroMagia = Time.time + cooldownMagia;
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
                // SÓ DÁ DASH SE TIVER FÔLEGO (AZUL)
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
            direcaoOlhar = moveInput.normalized; // guarda para onde o jogador está virado
            AoMover?.Invoke();
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

    // Versão sem origem (usada pelo modo debug): não aplica knockback
    public void ReceberDano(float dano)
    {
        ReceberDano(dano, transform.position);
    }

    public void ReceberDano(float dano, Vector2 origem)
    {
        if (estaMorto || estaAtordoado || estaDandoDash) return;

        vidaTotal -= dano;

        if (vidaTotal < 0f)
        {
            vidaTotal = 0f;
        }

        AtualizarHUD();

        StartCoroutine(RotinaFlash());

        if (vidaTotal <= 0)
        {
            StartCoroutine(RotinaMorte());
        }
        else
        {
            animator.SetTrigger("Hurt");
            StartCoroutine(RotinaAtordoamento());

            // 🚨 KNOCKBACK: empurra o jogador para longe da fonte do dano
            Vector2 direcaoEmpurrao = (Vector2)transform.position - origem;
            if (direcaoEmpurrao.sqrMagnitude > 0.001f)
            {
                StartCoroutine(RotinaKnockback(direcaoEmpurrao.normalized));
            }
        }
    }

    private IEnumerator RotinaKnockback(Vector2 direcao)
    {
        estaSofrendoKnockback = true;
        rb.velocity = direcao * forcaKnockback;
        yield return new WaitForSeconds(duracaoKnockback);
        rb.velocity = Vector2.zero;
        estaSofrendoKnockback = false;
    }

    // 🚨 HIT-STOP: micro-congelamento ao acertar um inimigo, dá peso ao golpe
    public void AplicarHitStop()
    {
        if (!emHitStop) StartCoroutine(RotinaHitStop());
    }

    private IEnumerator RotinaHitStop()
    {
        if (jogoTerminou) yield break;
        emHitStop = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duracaoHitStop);
        if (!jogoTerminou) Time.timeScale = 1f;
        emHitStop = false;
    }

    // 🚨 TELA DE FIM DE JOGO
    public void MostrarVitoria()
    {
        if (botaoContinuar != null) botaoContinuar.style.display = DisplayStyle.None;
        FinalizarJogo("VITORIA!");
    }

    public void MostrarDerrota()
    {
        if (botaoContinuar != null) botaoContinuar.style.display = DisplayStyle.None;
        FinalizarJogo("VOCE MORREU");
    }

    // Fim de FASE (não de jogo): mostra "FASE CONCLUIDA" + botão CONTINUAR para a próxima cena
    public void MostrarFaseConcluida(string proximaCena)
    {
        cenaProxima = proximaCena;
        if (botaoContinuar != null) botaoContinuar.style.display = DisplayStyle.Flex;
        FinalizarJogo("FASE CONCLUIDA");
    }

    private void FinalizarJogo(string texto)
    {
        if (jogoTerminou) return;
        jogoTerminou = true;

        if (labelResultado != null) labelResultado.text = texto;
        if (telaFim != null) telaFim.style.display = DisplayStyle.Flex;

        Time.timeScale = 0f; // congela o jogo por trás da tela de fim
    }

    private void Continuar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(cenaProxima);
    }

    private void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void VoltarAoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }

    // 🚨 MAGIA À DISTÂNCIA: cria o projétil em runtime, mirando no mouse (ou na direção do olhar)
    private void LancarMagia()
    {
        Vector2 direcao = direcaoOlhar;
        if (Camera.main != null)
        {
            Vector3 mundo = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 d = (Vector2)mundo - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.04f) direcao = d.normalized;
        }

        GameObject proj = new GameObject("ProjetilMagico");
        proj.transform.position = transform.position + (Vector3)(direcao * 0.6f);

        SpriteRenderer prSr = proj.AddComponent<SpriteRenderer>();
        prSr.sprite = spriteProjetil != null ? spriteProjetil : GerarSpriteCirculo();
        prSr.color = corMagia;
        prSr.sortingOrder = 10;

        CircleCollider2D col = proj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;

        Rigidbody2D rbProj = proj.AddComponent<Rigidbody2D>();
        rbProj.gravityScale = 0f;
        rbProj.freezeRotation = true;
        rbProj.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        proj.AddComponent<ProjetilMagico>().Iniciar(direcao, danoMagia, velocidadeMagia, tempoVidaMagia);

        // Dispara a animação de conjuração (ligue o trigger "Magia" à animação thrust no Animator)
        animator.SetFloat("LastInputX", direcao.x);
        animator.SetFloat("LastInputY", direcao.y);
        animator.SetTrigger("Magia");

        AoLancarMagia?.Invoke();
    }

    // Gera um círculo branco simples para o projétil quando não há sprite atribuído
    private static Sprite GerarSpriteCirculo()
    {
        if (spriteCirculoCache != null) return spriteCirculoCache;
        int tam = 32;
        Texture2D tex = new Texture2D(tam, tam) { wrapMode = TextureWrapMode.Clamp };
        Vector2 centro = new Vector2(tam / 2f, tam / 2f);
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centro);
                tex.SetPixel(x, y, dist <= (tam / 2f - 1f) ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        tex.Apply();
        spriteCirculoCache = Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 64f);
        return spriteCirculoCache;
    }

    public void GanharEspecial()
    {
        if (especialAtual < especialMaximo)
        {
            especialAtual += ganhoPorAcerto;
            if (especialAtual > especialMaximo) especialAtual = especialMaximo;
            AtualizarHUD(); 
        }
    }

    // 🚨 CONTROLA O VISUAL DAS BARRAS COM MATEMÁTICA DE PORCENTAGEM SEGURA
    private void AtualizarHUD()
    {
        if (barraVida != null)
        {
            float pctVida = Mathf.Clamp(vidaTotal / vidaMaxima, 0f, 1f);
            barraVida.transform.scale = new Vector3(pctVida, 1f, 1f);
        }
        
        if (barraFolego != null)
        {
            float pctFolego = Mathf.Clamp(folegoAtual / folegoMaximo, 0f, 1f);
            barraFolego.transform.scale = new Vector3(pctFolego, 1f, 1f);
        }
        
        if (barraEspecial != null)
        {
            float pctEspecial = Mathf.Clamp(especialAtual / especialMaximo, 0f, 1f);
            barraEspecial.transform.scale = new Vector3(pctEspecial, 1f, 1f);
        }
    }

    // --- CORROTINAS ---
    private IEnumerator RotinaDash(Vector2 direcao)
    {
        estaDandoDash = true;
        AoDarDash?.Invoke();
        animator.SetBool("isWalking", false);
        AtualizarHUD();
        
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Inimigo"), true);

        rb.velocity = direcao * velocidadeDash;
        yield return new WaitForSeconds(tempoDash);
        
        rb.velocity = Vector2.zero;
        estaDandoDash = false;

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Inimigo"), false);
    }

    private IEnumerator RotinaAtaqueEspecial(Vector2 direcao)
    {
        estaDandoDash = true;
        AoUsarEspecial?.Invoke();
        especialAtual = 0;
        AtualizarHUD();

        animator.SetTrigger("Attack"); 
        sr.color = Color.cyan; 

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

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Inimigo"), false);
    }

    private IEnumerator RotinaAtaque()
    {
        estaAtacando = true;
        AoAtacar?.Invoke();
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
        MostrarDerrota();
    }

    private void OnDestroy()
    {
        if (botaoContinuar != null) botaoContinuar.clicked -= Continuar;
        if (botaoReiniciar != null) botaoReiniciar.clicked -= Reiniciar;
        if (botaoMenu != null) botaoMenu.clicked -= VoltarAoMenu;
    }
}