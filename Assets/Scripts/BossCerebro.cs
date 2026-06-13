using UnityEngine;

public class BossCerebro : MonoBehaviour
{
    [Header("Configurações")]
    public float velocidade = 3.0f;
    public float raioDeteccao = 10f;

    private Transform alvoJogador;
    private Rigidbody2D rb;
    private SpriteRenderer sr; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); 

        // Procura quem é o jogador na cena
        GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jogadorObj != null) 
        {
            alvoJogador = jogadorObj.transform;
        }
    }

    void FixedUpdate()
    {
        if (alvoJogador == null) return;

        float distancia = Vector2.Distance(transform.position, alvoJogador.position);

        // Se o guerreiro chegar perto, o Boss começa a andar
        if (distancia <= raioDeteccao)
        {
            Vector2 direcao = (alvoJogador.position - transform.position).normalized;
            rb.MovePosition(rb.position + direcao * velocidade * Time.fixedDeltaTime);

            // O Truque do Espelho (FlipX)
            if (direcao.x > 0) 
            {
                sr.flipX = true; // Olha para a direita
            }
            else if (direcao.x < 0) 
            {
                sr.flipX = false; // Olha para a esquerda
            }
        }
        else
        {
            rb.velocity = Vector2.zero; // Fica parado se o guerreiro fugir
        }
    }
}