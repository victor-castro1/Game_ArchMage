using UnityEngine;
using UnityEngine.SceneManagement;

// Porta: leva o jogador para uma fase de combate ao apertar a tecla de interação.
// Pode ser colocada na cena ou criada em runtime (ver GerenciadorOndas).
public class Porta : MonoBehaviour
{
    [Tooltip("Cena carregada ao entrar nesta porta.")]
    public string cenaDestino = "Fase1";

    [Tooltip("Se ligado, precisa apertar a tecla perto. Se desligado, entra ao encostar.")]
    public bool exigirInteracao = true;
    public KeyCode teclaInteracao = KeyCode.E;

    private bool jogadorPerto = false;
    private bool carregando = false;
    private SpriteRenderer sr;
    private Color corBase = Color.white;
    private MovimentoJogador jogador;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) corBase = sr.color;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        jogadorPerto = true;
        jogador = col.GetComponentInParent<MovimentoJogador>();
        if (sr != null) sr.color = corBase * 1.4f; // realça a porta

        if (exigirInteracao)
        {
            if (jogador != null) jogador.MostrarDicaPorta(true);
        }
        else
        {
            Entrar();
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        jogadorPerto = false;
        if (sr != null) sr.color = corBase;
        if (jogador != null) jogador.MostrarDicaPorta(false);
    }

    private void Update()
    {
        if (exigirInteracao && jogadorPerto && Input.GetKeyDown(teclaInteracao)) Entrar();
    }

    private void Entrar()
    {
        if (carregando || string.IsNullOrEmpty(cenaDestino)) return;
        carregando = true;
        if (jogador != null)
        {
            jogador.MostrarDicaPorta(false);
            jogador.SalvarEstadoParaProximaFase(); // mantém vida/especial/fôlego, como o CONTINUAR
        }
        SceneManager.LoadScene(cenaDestino);
    }
}
