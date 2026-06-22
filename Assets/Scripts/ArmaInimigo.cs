using UnityEngine;

public class ArmaInimigo : MonoBehaviour
{
    public float danoDoAtaque = 15f; // Cada monstro pode ter um dano diferente lá no Inspector
    [Tooltip("Tempo mínimo entre dois danos enquanto o jogador continua encostado.")]
    public float intervaloDano = 1f;

    private float proximoDano = 0f;

    // OnTriggerStay (em vez de Enter) garante dano repetido enquanto o jogador fica em contato,
    // mesmo parado. O cooldown evita dano contínuo todo frame.
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Time.time < proximoDano) return;
        if (!collision.CompareTag("Player")) return;

        MovimentoJogador player = collision.GetComponent<MovimentoJogador>();
        if (player != null)
        {
            // Passa a posição do atacante para empurrar o jogador na direção certa (knockback)
            player.ReceberDano(danoDoAtaque, transform.position);
            proximoDano = Time.time + intervaloDano;
        }
    }
}
