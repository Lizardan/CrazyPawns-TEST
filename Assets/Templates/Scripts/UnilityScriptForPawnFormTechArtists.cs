using CrazyPawn;
using UnityEngine;

public class UnilityScriptForPawnFormTechArtists : MonoBehaviour
{
    CrazyPawnSettings settings;
    float boardHalf;
    Renderer[] renderers;
    Material[] originals;
    UtilityScriptForConnectorFromTechArtist[] connectors;
    Vector3 dragOffset;

    public void Init(CrazyPawnSettings s, float half)
    {
        settings = s;
        boardHalf = half;
        renderers = GetComponentsInChildren<Renderer>();
        originals = new Material[renderers.Length];
        connectors = new UtilityScriptForConnectorFromTechArtist[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originals[i] = renderers[i].sharedMaterial;
            connectors[i] = renderers[i].GetComponent<UtilityScriptForConnectorFromTechArtist>();
        }
    }

    public void BeginDrag(Vector3 ground) => dragOffset = transform.position - ground;

    public void Drag(Vector3 ground)
    {
        var p = ground + dragOffset;
        transform.position = new Vector3(p.x, 0f, p.z);
        UpdateMaterial();
    }

    public void EndDrag()
    {
        if (OffBoard())
        {
            PawnFieldGame.Instance?.OnPawnDestroyed(this);
            Destroy(gameObject);
            return;
        }
        UpdateMaterial();
    }

    bool OffBoard()
    {
        var p = transform.position;
        return Mathf.Abs(p.x) > boardHalf || Mathf.Abs(p.z) > boardHalf;
    }

    void UpdateMaterial()
    {
        var off = OffBoard();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (connectors[i])
            {
                if (off) renderers[i].sharedMaterial = settings.DeleteMaterial;
                else connectors[i].ApplyMaterial();
                continue;
            }
            renderers[i].sharedMaterial = off ? settings.DeleteMaterial : originals[i];
        }
    }
}
