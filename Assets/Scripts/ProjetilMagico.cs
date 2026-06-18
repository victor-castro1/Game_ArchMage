using UnityEngine;

// Projétil de magia à distância lançado pelo jogador.
// Viaja em linha reta, causa dano em Boss/Minion e é destruído ao bater em parede ou ao expirar.
public class ProjetilMagico : MonoBehaviour
{
    private Vector2 direcao;
    private float dano;
    private float velocidade;
    private Rigidbody2D rb;

    public void Iniciar(Vector2 dir, float dano, float velocidade, float tempoVida)
    {
        this.direcao = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.down;
        this.dano = dano;
        this.velocidade = velocidade;
        Destroy(gameObject, tempoVida);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = direcao * velocidade;

        // Aponta o sprite na direção do tiro
        float ang = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player")) return; // não acerta quem lançou

        if (col.CompareTag("Boss"))
        {
            col.GetComponentInParent<BossCerebro>()?.ReceberDano(dano);
            Destroy(gameObject);
            return;
        }

        if (col.CompareTag("Minion"))
        {
            col.GetComponentInParent<SlimeIA>()?.ReceberDano(dano);
            Destroy(gameObject);
            return;
        }

        // Bateu numa parede
        if (col.gameObject.layer == LayerMask.NameToLayer("Obstaculo"))
            Destroy(gameObject);
    }
}
