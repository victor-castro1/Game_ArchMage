using UnityEngine;
using System.Collections;

public class BossTesteHitbox : MonoBehaviour
{
    [Header("Status de Teste")]
    public float vidaTotal = 100f;

    [Header("Efeito de Dano (Feedback Visual)")]
    public Material materialFlash; 
    private Material materialOriginal;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            materialOriginal = sr.material;
        }
        
        Debug.Log("[HITBOX TEST] Esqueleto de teste pronto e estático. Aguardando ataques.");
    }

    public void ReceberDano(float quantidadeDano)
    {
        vidaTotal -= quantidadeDano;

        // 🚨 O LOG DETALHADO NO CONSOLE
        Debug.Log($"<color=yellow>[HITBOX INDENTIFICADA]</color> O Esqueleto sofreu {quantidadeDano} de dano! Vida restante: {vidaTotal}");

        // Pisca em branco para dar o feedback visual instantâneo na tela do jogo
        if (sr != null && materialFlash != null)
        {
            StartCoroutine(RotinaFlash());
        }
    }

    private IEnumerator RotinaFlash()
    {
        sr.material = materialFlash;
        yield return new WaitForSeconds(0.1f);
        sr.material = materialOriginal;
    }
}