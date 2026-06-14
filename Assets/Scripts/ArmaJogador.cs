using UnityEngine;

public class ArmaJogador : MonoBehaviour
{
    public float danoDoAtaque = 25f; // Ajuste o dano da sua foice aqui

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se a foice bater no Boss (Real ou de Teste)
        if (collision.CompareTag("Boss"))
        {
            // Tenta dar dano no Boss Real
            BossCerebro bossReal = collision.GetComponent<BossCerebro>();
            if (bossReal != null) bossReal.ReceberDano(danoDoAtaque);

            // Tenta dar dano no Boss de Teste (Novo)
            BossTesteHitbox bossTeste = collision.GetComponent<BossTesteHitbox>();
            if (bossTeste != null) bossTeste.ReceberDano(danoDoAtaque);
        }

        // Se a foice bater em um Slime
        if (collision.CompareTag("Minion"))
        {
            SlimeIA slime = collision.GetComponent<SlimeIA>();
            if (slime != null) slime.ReceberDano(danoDoAtaque);
        }
    }
}