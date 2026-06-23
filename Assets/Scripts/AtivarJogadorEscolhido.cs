using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coloque este script em um GameObject vazio (ex: "GameManager") em TODAS as suas fases.
/// Ele lê PlayerPrefs("ClasseEscolhida"), busca os personagens automaticamente na cena
/// e redireciona a câmera e o HUD para o personagem ativo.
/// </summary>
public class AtivarJogadorEscolhido : MonoBehaviour
{
    [Header("Câmera")]
    [Tooltip("Arraste a Cinemachine Virtual Camera da cena aqui. Se deixar vazio, o script a encontra automaticamente.")]
    public CinemachineVirtualCamera virtualCamera;

    void Awake()
    {
        AplicarClasseEscolhida();
    }

    private void AplicarClasseEscolhida()
    {
        // Lê a classe salva no menu/cutscene (Mago ou Paladino)
        string classeEscolhida = PlayerPrefs.GetString("ClasseEscolhida", "Paladino");
        Debug.Log($"[AtivarJogadorEscolhido] Aplicando classe '{classeEscolhida}' na cena {SceneManager.GetActiveScene().name}");

        GameObject objAtivar = null;
        GameObject objDesativar = null;

        // Encontra todos os componentes MovimentoJogador da cena (mesmo os que estão inativos/escondidos)
        MovimentoJogador[] todosJogadores = Resources.FindObjectsOfTypeAll<MovimentoJogador>();

        foreach (MovimentoJogador jogador in todosJogadores)
        {
            // Ignora prefabs nos assets e objetos de outras cenas
            if (jogador.gameObject.scene != SceneManager.GetActiveScene()) continue;

            if (jogador.ehMago)
            {
                if (classeEscolhida == "Mago") objAtivar = jogador.gameObject;
                else objDesativar = jogador.gameObject;
            }
            else // Paladino (ehMago == false)
            {
                if (classeEscolhida == "Paladino" || string.IsNullOrEmpty(classeEscolhida)) objAtivar = jogador.gameObject;
                else objDesativar = jogador.gameObject;
            }
        }

        // Executa a ativação/desativação
        if (objDesativar != null)
        {
            objDesativar.SetActive(false);
        }

        if (objAtivar != null)
        {
            objAtivar.SetActive(true);

            // Garante que a câmera seja encontrada
            if (virtualCamera == null)
                virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

            // Redireciona a câmera para o personagem ativo
            if (virtualCamera != null)
            {
                virtualCamera.Follow = objAtivar.transform;
                Debug.Log($"[AtivarJogadorEscolhido] Câmera redirecionada para: {objAtivar.name}");
            }
        }
        else
        {
            Debug.LogWarning("[AtivarJogadorEscolhido] Não foi possível encontrar os personagens na cena. Verifique se eles possuem o script MovimentoJogador.");
        }
    }
}

