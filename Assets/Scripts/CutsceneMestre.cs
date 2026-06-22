using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

// Cutscene de introdução ("Mundo Real — a Mesa"): o Mestre apresenta a missão
// e o jogador escolhe entre Mago e Paladino antes de cair na Fase 1.
public class CutsceneMestre : MonoBehaviour
{
    [Header("Configurações")]
    public string proximaCena = "Fase1";
    [Tooltip("Tempo do fade antes de carregar a próxima cena.")]
    public float tempoFade = 1f;

    [Header("Arte (opcional - arraste sprites no Inspector)")]
    [Tooltip("Imagem de fundo da cutscene inteira.")]
    public Sprite arteFundo;
    [Tooltip("Retrato exibido quando o MESTRE fala.")]
    public Sprite retratoMestre;
    [Tooltip("Retrato exibido quando o PROTAGONISTA (VOCÊ) fala.")]
    public Sprite retratoProtagonista;

    private UIDocument doc;
    private Label lblFalante;
    private Label lblTexto;
    private VisualElement painelDialogo;
    private VisualElement painelEscolha;
    private VisualElement fadeTela;
    private VisualElement retrato;
    private VisualElement fundo;
    private Button botaoMago;
    private Button botaoPaladino;

    // Roteiro condensado (baseado em "HISTÓRIA E FASES")
    private struct Fala { public string quem; public string texto; public Fala(string q, string t){ quem=q; texto=t; } }
    private readonly Fala[] roteiro = new Fala[]
    {
        new Fala("JOANA", "Pode entrar. A porta tá aberta."),
        new Fala("VOCÊ", "...Mestre, tá trancada."),
        new Fala("JOANA", "Hah! Trancada, é? Deixa comigo — essa porta sempre emperra."),
        new Fala("JOANA", "Pronto. Senta aqui. Hoje a brincadeira é de verdade."),
        new Fala("VOCÊ", "De verdade como? Qual é a missão?"),
        new Fala("JOANA", "Tá vendo esse planeta azul? Ele não devia estar aí."),
        new Fala("JOANA", "Algo despertou lá no fundo dele. E está subindo."),
        new Fala("JOANA", "Você vai descer até o Núcleo e silenciar isso."),
        new Fala("JOANA", "Falar é fácil. Sobreviver até lá, nem tanto."),
        new Fala("JOANA", "E não esquece: aqui dentro, toda escolha cobra um preço."),
        new Fala("JOANA", "Agora me diz... quem você quer ser?"),
    };

    private int indice = 0;
    private bool emEscolha = false;
    private bool carregando = false;

    void Start()
    {
        Time.timeScale = 1f; // segurança

        doc = GetComponent<UIDocument>();
        if (doc == null) { Debug.LogError("[CutsceneMestre] Sem UIDocument."); return; }

        var root = doc.rootVisualElement;
        lblFalante = root.Q<Label>("lbl-falante");
        lblTexto = root.Q<Label>("lbl-texto");
        painelDialogo = root.Q<VisualElement>("painel-dialogo");
        painelEscolha = root.Q<VisualElement>("painel-escolha");
        fadeTela = root.Q<VisualElement>("fade-tela");
        retrato = root.Q<VisualElement>("retrato");
        fundo = root.Q<VisualElement>("cutscene-root");
        botaoMago = root.Q<Button>("btn-mago");
        botaoPaladino = root.Q<Button>("btn-paladino");

        if (botaoMago != null) botaoMago.clicked += () => Escolher("Mago");
        if (botaoPaladino != null) botaoPaladino.clicked += () => Escolher("Paladino");

        if (painelEscolha != null) painelEscolha.style.display = DisplayStyle.None;
        if (fadeTela != null) fadeTela.style.opacity = 0f;

        // Arte de fundo opcional
        if (fundo != null && arteFundo != null)
            fundo.style.backgroundImage = new StyleBackground(arteFundo);

        MostrarFala();
    }

    void Update()
    {
        if (carregando || emEscolha) return;

        // Avança o diálogo com clique, espaço ou enter
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            Avancar();
        }
    }

    private void MostrarFala()
    {
        if (indice >= roteiro.Length) { AbrirEscolha(); return; }
        if (lblFalante != null) lblFalante.text = roteiro[indice].quem;
        if (lblTexto != null) lblTexto.text = roteiro[indice].texto;

        // Troca o retrato conforme quem está falando (Mestre à esquerda, Você à direita)
        if (retrato != null)
        {
            bool eMestre = roteiro[indice].quem != "VOCÊ";
            Sprite s = eMestre ? retratoMestre : retratoProtagonista;
            if (s != null)
            {
                retrato.style.backgroundImage = new StyleBackground(s);
                retrato.style.display = DisplayStyle.Flex;
                retrato.style.left = eMestre ? new StyleLength(Length.Percent(6)) : new StyleLength(StyleKeyword.Auto);
                retrato.style.right = eMestre ? new StyleLength(StyleKeyword.Auto) : new StyleLength(Length.Percent(6));
            }
            else
            {
                retrato.style.display = DisplayStyle.None;
            }
        }
    }

    private void Avancar()
    {
        indice++;
        if (indice >= roteiro.Length) AbrirEscolha();
        else MostrarFala();
    }

    private void AbrirEscolha()
    {
        emEscolha = true;
        if (painelDialogo != null) painelDialogo.style.display = DisplayStyle.None;
        if (painelEscolha != null) painelEscolha.style.display = DisplayStyle.Flex;
    }

    private void Escolher(string classe)
    {
        if (carregando) return;
        carregando = true;
        MovimentoJogador.LimparEstadoSalvo(); // novo run começa com vida/especial cheios
        PlayerPrefs.SetString("ClasseEscolhida", classe);
        PlayerPrefs.Save();
        Debug.Log("[CutsceneMestre] Classe escolhida: " + classe);
        StartCoroutine(RotinaSair());
    }

    private IEnumerator RotinaSair()
    {
        if (fadeTela != null) fadeTela.style.opacity = 1f;
        yield return new WaitForSeconds(tempoFade);
        SceneManager.LoadScene(proximaCena);
    }
}
