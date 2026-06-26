using UnityEngine;

public class UtilityScriptForConnectorFromTechArtist : MonoBehaviour
{
    const float PulseSpeed = 4f;
    const float PulseAmount = 0.12f;

    Renderer rend;
    Material original;
    Vector3 baseScale;
    bool highlighted, waitingConnect;

    [HideInInspector] public UnilityScriptForPawnFormTechArtists Pawn;

    void Awake()
    {
        Pawn = GetComponentInParent<UnilityScriptForPawnFormTechArtists>();
        rend = GetComponent<Renderer>();
        original = rend.sharedMaterial;
        baseScale = transform.localScale;
    }

    public void SetWaitingConnect(bool on)
    {
        waitingConnect = on;
        if (!on) transform.localScale = baseScale;
    }

    void Update()
    {
        if (!waitingConnect) return;
        var s = 1f + Mathf.Sin(Time.time * PulseSpeed) * PulseAmount;
        transform.localScale = baseScale * s;
    }

    public void SetHighlighted(bool on)
    {
        highlighted = on;
        ApplyMaterial();
    }

    public void ApplyMaterial()
    {
        if (!rend) return;
        rend.sharedMaterial = highlighted
            ? PawnFieldGame.Instance.settings.ActiveConnectorMaterial
            : original;
    }
}
