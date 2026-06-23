using UnityEngine;
using Cinemachine;

/// <summary>
/// Coloque este script em um GameObject vazio na cena (ex: "GameManager").
/// Ele lê PlayerPrefs("ClasseEscolhida"), ativa o personagem correto
/// e redireciona o Follow da Cinemachine Virtual Camera para ele.
///
/// Valores esperados em PlayerPrefs:
///   "Paladino"  → ativa Jogador1, desativa Mago
///   "Mago"      → ativa Mago, desativa Jogador1
///   (vazio)     → fallback para Paladino
/// </summary>
public class AtivarJogadorEscolhido : MonoBehaviour
{
    [Header("Referências dos Personagens")]
    [Tooltip("Arraste o GameObject 'Jogador1' (Paladino) aqui.")]
    public GameObject jogador1Paladino;

    [Tooltip("Arraste o GameObject 'Mago' aqui.")]
    public GameObject mago;

    [Header("Câmera")]
    [Tooltip("Arraste a Cinemachine Virtual Camera da cena aqui. " +
             "Se deixar vazio, o script tenta encontrá-la automaticamente.")]
    public CinemachineVirtualCamera virtualCamera;

    void Awake()
    {
        // Tenta encontrar a Virtual Camera automaticamente se não foi atribuída
        if (virtualCamera == null)
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (virtualCamera == null)
            Debug.LogWarning("[AtivarJogadorEscolhido] Nenhuma CinemachineVirtualCamera encontrada na cena. " +
                             "A câmera não será redirecionada.");

        // Lê a classe salva no menu de seleção
        string classeEscolhida = PlayerPrefs.GetString("ClasseEscolhida", "Paladino");

        switch (classeEscolhida)
        {
            case "Mago":
                AtivarPersonagem(mago, jogador1Paladino);
                break;

            case "Paladino":
            default:
                AtivarPersonagem(jogador1Paladino, mago);
                break;
        }

        Debug.Log($"[AtivarJogadorEscolhido] Classe carregada: '{classeEscolhida}'");
    }

    /// <summary>
    /// Ativa <paramref name="ativar"/>, desativa <paramref name="desativar"/>
    /// e aponta o Follow da câmera para o personagem ativo.
    /// </summary>
    private void AtivarPersonagem(GameObject ativar, GameObject desativar)
    {
        if (desativar != null)
            desativar.SetActive(false);
        else
            Debug.LogWarning("[AtivarJogadorEscolhido] Referência do personagem a DESATIVAR está nula. Verifique o Inspector.");

        if (ativar != null)
        {
            ativar.SetActive(true);

            // Redireciona a câmera para o personagem que acabou de ser ativado
            if (virtualCamera != null)
            {
                virtualCamera.Follow = ativar.transform;
                Debug.Log($"[AtivarJogadorEscolhido] Câmera redirecionada para: {ativar.name}");
            }
        }
        else
        {
            Debug.LogWarning("[AtivarJogadorEscolhido] Referência do personagem a ATIVAR está nula. Verifique o Inspector.");
        }
    }
}

