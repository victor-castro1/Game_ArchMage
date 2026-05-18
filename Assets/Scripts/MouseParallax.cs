using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseParallax : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private float parallaxModifier = 0.05f; // Quanto maior, mais a camada se move
    
    private Vector2 startPosition;
    void Start()
    {
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        
        // Centraliza o valor para que o meio da tela seja (0,0)
        float offsetX = (mousePosition.x - 0.5f) * parallaxModifier;
        float offsetY = (mousePosition.y - 0.5f) * parallaxModifier;

        // Aplica a nova posição suavemente com base na posição inicial
        transform.position = new Vector3(startPosition.x + offsetX, startPosition.y + offsetY, transform.position.z);
    }
}
