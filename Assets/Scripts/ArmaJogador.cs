using UnityEngine;

public class ArmaJogador : MonoBehaviour
{
    public float danoDoAtaque = 25f; // Ajuste o dano da sua foice aqui

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se a foice bater na Hitbox do Boss (que agora é um objeto filho)
        if (collision.CompareTag("Boss"))
        {
            // O 'InParent' sobe na hierarquia para achar o BossCerebro no objeto principal!
            BossCerebro boss = collision.GetComponentInParent<BossCerebro>();
            if (boss != null) boss.ReceberDano(danoDoAtaque);
        }

        // Se a foice bater na Hitbox de um Slime
        if (collision.CompareTag("Minion"))
        {
            SlimeIA slime = collision.GetComponentInParent<SlimeIA>();
            if (slime != null) slime.ReceberDano(danoDoAtaque);
        }
    }
}