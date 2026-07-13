using UnityEngine;

public class MushlightInteract : MonoBehaviour
{
    private Light mushLight;
    private bool isOn = false;

    void Start()
    {
        // 1. Tambahkan Point Light ke mushlight
        GameObject lightObj = new GameObject("MushPointLight");
        lightObj.transform.SetParent(this.transform);
        
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Posisikan lampu di tengah-tengah / agak atas dari bounds jamur
            lightObj.transform.position = renderer.bounds.center + new Vector3(0, 0.5f, 0); 
        }
        else
        {
            lightObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        }
        
        mushLight = lightObj.AddComponent<Light>();
        mushLight.type = LightType.Point;
        mushLight.color = new Color(1f, 0.8f, 0.1f); // Cahaya kuning
        mushLight.intensity = 15f; // Intensitas cukup untuk menerangi lingkungan lokal
        mushLight.range = 20f; // Jangkauan cahaya yang luas
        mushLight.enabled = false;
        mushLight.shadows = LightShadows.Soft; // Tambahkan bayangan agar lebih realistis

        // Tambahkan BoxCollider jika belum ada
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(3, 4, 3);
            box.center = new Vector3(0, 2, 0);
        }
    }

    void OnMouseDown()
    {
        ToggleLight();
    }

    public void ToggleLight()
    {
        if (isOn) return; // Mencegah diklik dua kali
        
        isOn = true;

        // Beri tahu puzzle manager bahwa jamur ini sudah menyala
        if (MushlightPuzzleManager.Instance != null)
        {
            MushlightPuzzleManager.Instance.OnMushlightTurnedOn();
        }

        // Nyalakan lampu kuning
        if (mushLight != null)
        {
            mushLight.enabled = true;
        }

        // Nyalakan emission HANYA pada bagian Glow jamur
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            foreach (var mat in renderer.materials)
            {
                // Hanya modifikasi material yang mengandung kata "Glow"
                if (mat.name.Contains("Glow"))
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.1f) * 2.5f); 
                    }
                }
            }
        }
    }
}
