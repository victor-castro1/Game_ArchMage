using UnityEngine;

public class ArmaJogador : MonoBehaviour
{
    public float danoDoAtaque = 25f; // Ajuste o dano da sua foice aqui

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se a foice bater na Hitbox do Boss (que agora é um objeto filho)
        if (collision.CompareTag("Boss"))
        {
            BossCerebro boss = collision.GetComponentInParent<BossCerebro>();
            if (boss != null)
            {
                boss.ReceberDano(danoDoAtaque);
                MovimentoJogador jogador = GetComponentInParent<MovimentoJogador>();
                if (jogador != null)
                {
                    jogador.GanharEspecial();
                    jogador.AplicarHitStop(); // peso no golpe
                }
            }
        }

        // Se a foice bater na Hitbox de um Slime
        if (collision.CompareTag("Minion"))
        {
            SlimeIA slime = collision.GetComponentInParent<SlimeIA>();
            if (slime != null)
            {
                slime.ReceberDano(danoDoAtaque);
                GetComponentInParent<MovimentoJogador>()?.AplicarHitStop();
            }
        }
    }
}