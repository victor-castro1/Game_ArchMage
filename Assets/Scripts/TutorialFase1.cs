using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Tutorial flutuante da Fase 1: mostra os controles e avança conforme o jogador
// executa cada ação. Tem timeout de segurança por passo e botão "Pular".
public class TutorialFase1 : MonoBehaviour
{
    [Tooltip("Tempo máximo (s) que cada passo espera antes de avançar sozinho.")]
    public float tempoMaximoPorPasso = 10f;

    private MovimentoJogador jogador;
    private VisualElement painel;
    private Label texto;
    private Button botaoPular;

    // Flags marcadas pelos eventos do jogador
    private bool moveu, atacou, lancouMagia, deuDash, usouEspecial;
    private bool pulado;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jogador = p.GetComponent<MovimentoJogador>();

        if (jogador == null || jogador.hudDocument == null)
        {
            Debug.LogWarning("[TutorialFase1] Jogador/HUD não encontrado; tutorial desativado.");
            enabled = false;
            return;
        }

        var root = jogador.hudDocument.rootVisualElement;
        painel = root.Q<VisualElement>("tutorial-panel");
        texto = root.Q<Label>("lbl-tutorial");
        botaoPular = root.Q<Button>("btn-pular-tutorial");

        if (botaoPular != null) botaoPular.clicked += Pular;

        // Escuta as ações do jogador
        jogador.AoMover += () => moveu = true;
        jogador.AoAtacar += () => atacou = true;
        jogador.AoLancarMagia += () => lancouMagia = true;
        jogador.AoDarDash += () => deuDash = true;
        jogador.AoUsarEspecial += () => usouEspecial = true;

        if (painel != null) painel.style.display = DisplayStyle.Flex;
        StartCoroutine(RotinaTutorial());
    }

    private IEnumerator RotinaTutorial()
    {
        yield return Passo("Use W A S D para se MOVER", () => moveu);
        yield return Passo("Clique ESQUERDO para ATACAR", () => atacou);
        yield return Passo("Clique DIREITO para lançar MAGIA (mira no mouse)", () => lancouMagia);
        yield return Passo("Aperte 2x a mesma direção para dar DASH", () => deuDash);
        yield return Passo("Barra verde cheia + andando: ESPAÇO = ESPECIAL", () => usouEspecial, 14f);

        EsconderPainel();
    }

    private IEnumerator Passo(string instrucao, System.Func<bool> concluido, float tempoMax = -1f)
    {
        if (pulado) yield break;
        if (tempoMax < 0f) tempoMax = tempoMaximoPorPasso;

        if (texto != null) texto.text = instrucao;

        float t = 0f;
        while (!pulado && !concluido() && t < tempoMax)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (pulado) yield break;

        if (texto != null) texto.text = "OK!  " + instrucao;
        yield return new WaitForSeconds(0.6f);
    }

    private void Pular()
    {
        pulado = true;
        StopAllCoroutines();
        EsconderPainel();
    }

    private void EsconderPainel()
    {
        if (painel != null) painel.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (botaoPular != null) botaoPular.clicked -= Pular;
    }
}
