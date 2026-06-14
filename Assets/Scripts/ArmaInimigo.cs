using UnityEngine;

public class ArmaInimigo : MonoBehaviour
{
    public float danoDoAtaque = 15f; // Cada monstro pode ter um dano diferente lá no Inspector

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se a hitbox bateu no Jogador
        if (collision.CompareTag("Player"))
        {
            MovimentoJogador player = collision.GetComponent<MovimentoJogador>();
            if (player != null)
            {
                player.ReceberDano(danoDoAtaque);
            }
        }
    }
}