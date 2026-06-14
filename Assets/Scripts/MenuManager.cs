using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    [Header("Configurações")]
    public string nomeCenaJogo = "SampleScene";

    private UIDocument menuDocument;
    private Button botaoJogar;
    private Button botaoSair;
    private VisualElement fadeTela; // 🚨 A nossa cortina preta

    void Start()
    {
        menuDocument = GetComponent<UIDocument>();

        if (menuDocument != null)
        {
            var root = menuDocument.rootVisualElement;

            botaoJogar = root.Q<Button>("btn-jogar");
            botaoSair = root.Q<Button>("btn-sair");
            fadeTela = root.Q<VisualElement>("fade-tela"); // 🚨 Encontra a cortina

            if (botaoJogar != null) botaoJogar.clicked += IniciarJogo;
            if (botaoSair != null) botaoSair.clicked += SairDoJogo;
        }
    }

    private void IniciarJogo()
    {
        // Trava o botão para o jogador não clicar duas vezes
        botaoJogar.SetEnabled(false); 

        if (fadeTela != null)
        {
            // Aciona a animação do UI Toolkit mudando a opacidade para 100%
            fadeTela.style.opacity = 1f;

            // Espera 1 segundo (o tempo da transição) e depois roda a função de carregar
            Invoke(nameof(CarregarCena), 1f);
        }
        else
        {
            // Se você esquecer de criar a cortina, carrega direto pra não quebrar
            CarregarCena();
        }
    }

    private void CarregarCena()
    {
        SceneManager.LoadScene(nomeCenaJogo);
    }

    private void SairDoJogo()
    {
        Debug.Log("Saindo do jogo..."); 
        Application.Quit();
    }

    private void OnDestroy()
    {
        if (botaoJogar != null) botaoJogar.clicked -= IniciarJogo;
        if (botaoSair != null) botaoSair.clicked -= SairDoJogo;
    }
}