using UnityEngine;

public class UnilityScriptForPawnFormTechArtists : MonoBehaviour
{
    Renderer[] renderers;
    Material[] originals;
    UtilityScriptForConnectorFromTechArtist[] connectors;
    Vector3 dragOffset;

    public void Init()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originals = new Material[renderers.Length];
        connectors = new UtilityScriptForConnectorFromTechArtist[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            connectors[i] = renderers[i].GetComponent<UtilityScriptForConnectorFromTechArtist>();
            if (!connectors[i]) originals[i] = renderers[i].sharedMaterial;
        }
    }

    public bool OwnsConnector(UtilityScriptForConnectorFromTechArtist connector)
    {
        if (!connector) return false;
        for (int i = 0; i < connectors.Length; i++)
            if (connectors[i] == connector) return true;
        return false;
    }

    public void SetConnectorsHighlighted(bool on)
    {
        for (int i = 0; i < connectors.Length; i++)
            if (connectors[i]) connectors[i].SetHighlighted(on);
    }

    public void BeginDrag(Vector3 ground) => dragOffset = transform.position - ground;

    public void Drag(Vector3 ground)
    {
        var p = ground + dragOffset;
        transform.position = new Vector3(p.x, 0f, p.z);
        UpdateMaterial(PawnFieldGame.Instance.IsOffBoard(transform.position));
    }

    public void EndDrag()
    {
        var off = PawnFieldGame.Instance.IsOffBoard(transform.position);
        if (off)
        {
            PawnFieldGame.Instance.OnPawnDestroyed(this);
            Destroy(gameObject);
            return;
        }
        UpdateMaterial(off);
    }

    void UpdateMaterial(bool off)
    {
        var delete = PawnFieldGame.Instance.settings.DeleteMaterial;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (connectors[i])
            {
                if (off) renderers[i].sharedMaterial = delete;
                else connectors[i].ApplyMaterial();
                continue;
            }
            renderers[i].sharedMaterial = off ? delete : originals[i];
        }
    }
}
