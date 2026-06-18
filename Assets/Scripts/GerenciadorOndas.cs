using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Gerencia as ondas de inimigos da Fase 1 (tutorial). Por padrão: 2 ondas de 6 slimes.
// Ao limpar todas as ondas, dispara a tela de "Fase Concluída" do jogador.
public class GerenciadorOndas : MonoBehaviour
{
    [Header("Inimigos")]
    [Tooltip("Prefab do slime (SlimeMinion).")]
    public GameObject prefabSlime;
    [Tooltip("Quantidade de slimes em cada onda. Ex: {6, 6} = 2 ondas de 6.")]
    public int[] slimesPorOnda = { 6, 6 };

    [Header("Spawn")]
    [Tooltip("Distância mínima em que os slimes nascem ao redor do jogador.")]
    public float raioSpawnMin = 2.5f;
    [Tooltip("Distância máxima em que os slimes nascem ao redor do jogador.")]
    public float raioSpawn = 6f;
    public float atrasoInicial = 1.5f;
    [Tooltip("'Safe time' entre uma onda e outra.")]
    public float tempoEntreOndas = 2.5f;

    [Header("Fluxo")]
    [Tooltip("Cena carregada ao concluir a fase (ex: a fase do boss).")]
    public string proximaCena = "SampleScene";

    private Transform jogador;

    void Awake()
    {
        // Esta cena foi duplicada da fase do boss: garante que o boss fique desligado aqui.
        BossCerebro boss = FindObjectOfType<BossCerebro>();
        if (boss != null) boss.gameObject.SetActive(false);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jogador = p.transform;
        StartCoroutine(RotinaFase());
    }

    private IEnumerator RotinaFase()
    {
        yield return new WaitForSeconds(atrasoInicial);

        for (int onda = 0; onda < slimesPorOnda.Length; onda++)
        {
            SpawnarOnda(slimesPorOnda[onda]);

            // Espera um frame para os slimes existirem antes de contar
            yield return null;

            while (GameObject.FindGameObjectsWithTag("Minion").Length > 0)
                yield return new WaitForSeconds(0.3f);

            if (onda < slimesPorOnda.Length - 1)
                yield return new WaitForSeconds(tempoEntreOndas);
        }

        FinalizarFase();
    }

    private void SpawnarOnda(int quantidade)
    {
        if (prefabSlime == null)
        {
            Debug.LogWarning("[GerenciadorOndas] prefabSlime não atribuído.");
            return;
        }

        Vector3 centro = jogador != null ? jogador.position : transform.position;
        int mascaraObstaculo = LayerMask.GetMask("Obstaculo");

        for (int i = 0; i < quantidade; i++)
        {
            Vector3 pos = EncontrarPosicaoValida(centro, mascaraObstaculo);
            Instantiate(prefabSlime, pos, Quaternion.identity);
        }
    }

    // Procura um ponto que esteja DENTRO da arena e com caminho livre até o jogador,
    // pra nenhum slime nascer atrás de parede / fora do mapa (e a onda travar).
    private Vector3 EncontrarPosicaoValida(Vector3 centro, int mascaraObstaculo)
    {
        for (int tentativa = 0; tentativa < 25; tentativa++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.up;
            float dist = Random.Range(raioSpawnMin, raioSpawn);
            Vector3 pos = centro + (Vector3)(dir * dist);

            // 1) não pode nascer dentro de uma parede
            if (Physics2D.OverlapCircle(pos, 0.4f, mascaraObstaculo)) continue;

            // 2) precisa ter linha livre até o jogador (garante que está na arena e é alcançável)
            Vector2 ateJogador = centro - pos;
            if (Physics2D.Raycast(pos, ateJogador.normalized, ateJogador.magnitude, mascaraObstaculo).collider != null)
                continue;

            return pos;
        }

        // Fallback: bem perto do jogador (sempre alcançável)
        Vector2 fallback = Random.insideUnitCircle.normalized;
        if (fallback == Vector2.zero) fallback = Vector2.up;
        return centro + (Vector3)(fallback * raioSpawnMin);
    }

    private void FinalizarFase()
    {
        MovimentoJogador player = jogador != null ? jogador.GetComponent<MovimentoJogador>() : null;
        if (player != null)
            player.MostrarFaseConcluida(proximaCena);
        else
            SceneManager.LoadScene(proximaCena); // fallback
    }
}
