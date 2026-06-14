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
                GetComponentInParent<MovimentoJogador>().GanharEspecial();
            }
        }

        // Se a foice bater na Hitbox de um Slime
        if (collision.CompareTag("Minion"))
        {
            SlimeIA slime = collision.GetComponentInParent<SlimeIA>();
            if (slime != null) slime.ReceberDano(danoDoAtaque);
        }
    }
}