using UnityEngine;

namespace SummerBackgroundsPixelArt
{
    public class ParallaxEffect : MonoBehaviour
    {
        private Transform mainCamera;

        [Header("Configurações do Mouse")]
        public float parallaxIntensityX = 0.05f;
        public float parallaxIntensityY = 0.05f;

        private Vector2 initialPos;

        private void Start()
        {
            mainCamera = Camera.main.transform;
            
            // Salva a posição inicial onde você posicionou o layer na Unity
            initialPos = transform.position;
        }

        private void LateUpdate()
        {
            // Pega a posição do mouse em relação à tela (valores de 0 a 1)
            Vector3 mousePosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
            
            // Centraliza o valor para que o meio da tela seja o ponto zero (0,0)
            float mouseOffsetX = mousePosition.x - 0.5f;
            float mouseOffsetY = mousePosition.y - 0.5f;

            // Calcula o deslocamento multiplicando a posição do mouse pela intensidade definida no Inspector
            float parallaxOffsetX = mouseOffsetX * parallaxIntensityX;
            float parallaxOffsetY = mouseOffsetY * parallaxIntensityY;

            // Aplica o movimento mantendo o Z do objeto intacto
            transform.position = new Vector3(initialPos.x + parallaxOffsetX, initialPos.y + parallaxOffsetY, transform.position.z);
        }
    }
}