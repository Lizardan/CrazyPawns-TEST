using UnityEngine;

public class UtilityScriptForConnectorFromTechArtist : MonoBehaviour
{
    static Material activeMat;

    Renderer rend;
    Material original;
    bool highlighted;

    public UnilityScriptForPawnFormTechArtists Pawn { get; private set; }

    public static void SetActiveMaterial(Material mat) => activeMat = mat;

    void Awake()
    {
        Pawn = GetComponentInParent<UnilityScriptForPawnFormTechArtists>();
        rend = GetComponent<Renderer>();
        original = rend.sharedMaterial;
    }

    public void SetHighlighted(bool on)
    {
        highlighted = on;
        ApplyMaterial();
    }

    public void ApplyMaterial()
    {
        if (!rend) return;
        rend.sharedMaterial = highlighted ? activeMat : original;
    }
}
