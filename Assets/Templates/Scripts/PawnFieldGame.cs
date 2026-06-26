using System.Collections.Generic;
using CrazyPawn;
using UnityEngine;

public class PawnFieldGame : MonoBehaviour
{
    public static PawnFieldGame Instance { get; private set; }

    [SerializeField] CrazyPawnSettings settings;
    [SerializeField] GameObject pawnPrefab;
    [SerializeField] GameObject cellPrefab;
    [SerializeField] Material linkMaterial;

    const float CellSize = 1.5f;
    const float ScrollSpeed = 2f;
    const float ZoomSmoothing = 12f;

    float boardHalf;
    Camera cam;
    Material cellTemplate;
    Material boardBlack;
    Material boardWhite;

    UnilityScriptForPawnFormTechArtists dragPawn;
    bool panning;
    Vector3 panGround;
    Vector3 panCam;
    Vector2 mouseDown;

    UtilityScriptForConnectorFromTechArtist connectFrom;
    bool connecting;
    bool dragConnect;
    bool connectorClick;

    float zoomImpulse;

    readonly List<(UtilityScriptForConnectorFromTechArtist a, UtilityScriptForConnectorFromTechArtist b, LineRenderer line)> links = new();

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
        UtilityScriptForConnectorFromTechArtist.SetActiveMaterial(settings.ActiveConnectorMaterial);
        cellTemplate = cellPrefab.GetComponent<Renderer>().sharedMaterial;
    }

    void Start()
    {
        boardHalf = settings.CheckerboardSize * CellSize * 0.5f;
        CreateBoard();
        SpawnPawns();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (boardBlack) Destroy(boardBlack);
        if (boardWhite) Destroy(boardWhite);
        foreach (var (_, _, line) in links)
            if (line) Destroy(line.gameObject);
        links.Clear();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) MouseDown();
        if (Input.GetMouseButton(0)) MouseDrag();
        if (Input.GetMouseButtonUp(0)) MouseUp();
        zoomImpulse += Input.mouseScrollDelta.y;
        ApplyZoom();
    }

    void LateUpdate()
    {
        for (int i = links.Count - 1; i >= 0; i--)
        {
            var (a, b, line) = links[i];
            if (!a || !b)
            {
                if (line) Destroy(line.gameObject);
                links.RemoveAt(i);
                continue;
            }
            line.SetPosition(0, a.transform.position);
            line.SetPosition(1, b.transform.position);
        }
    }

    void CreateBoard()
    {
        var root = new GameObject("Board");
        var origin = boardHalf - CellSize * 0.5f;
        boardBlack = NewCellMat(settings.BlackCellColor);
        boardWhite = NewCellMat(settings.WhiteCellColor);

        for (int x = 0; x < settings.CheckerboardSize; x++)
            for (int z = 0; z < settings.CheckerboardSize; z++)
            {
                var cell = Instantiate(cellPrefab, root.transform);
                cell.transform.position = new Vector3(-origin + x * CellSize, 0f, -origin + z * CellSize);
                cell.GetComponent<Renderer>().sharedMaterial = (x + z) % 2 == 0 ? boardBlack : boardWhite;
            }
    }

    Material NewCellMat(Color color)
    {
        var mat = new Material(cellTemplate);
        mat.SetColor("_BaseColor", color);
        return mat;
    }

    void SpawnPawns()
    {
        for (int i = 0; i < settings.InitialPawnCount; i++)
        {
            var p = UnityEngine.Random.insideUnitCircle * settings.InitialZoneRadius;
            var go = Instantiate(pawnPrefab, new Vector3(p.x, 0f, p.y), Quaternion.identity);
            go.GetComponent<UnilityScriptForPawnFormTechArtists>().Init(settings, boardHalf);
        }
    }

    void MouseDown()
    {
        mouseDown = Input.mousePosition;
        connectorClick = false;

        var col = UnderMouse();
        if (col)
        {
            var connector = col.GetComponent<UtilityScriptForConnectorFromTechArtist>();
            if (connector)
            {
                if (connecting && !dragConnect) EndConnect(connector);
                else if (!connecting) StartConnect(connector);
                connectorClick = true;
                return;
            }

            var pawn = col.GetComponentInParent<UnilityScriptForPawnFormTechArtists>();
            if (pawn)
            {
                dragPawn = pawn;
                dragPawn.BeginDrag(GroundUnderMouse(cam.transform.position));
                return;
            }
        }

        if (connecting && !dragConnect) EndConnect(null);

        panning = true;
        panCam = cam.transform.position;
        panGround = GroundUnderMouse(panCam);
    }

    void MouseDrag()
    {
        if (connectorClick && connecting && ((Vector2)Input.mousePosition - mouseDown).sqrMagnitude > 25f)
            dragConnect = true;

        if (dragPawn)
            dragPawn.Drag(GroundUnderMouse(cam.transform.position));

        if (!panning) return;

        var delta = panGround - GroundUnderMouse(panCam);
        cam.transform.position = panCam + new Vector3(delta.x, 0f, delta.z);
    }

    void MouseUp()
    {
        if (dragConnect && connecting)
        {
            var col = UnderMouse();
            EndConnect(col ? col.GetComponent<UtilityScriptForConnectorFromTechArtist>() : null);
        }

        dragPawn?.EndDrag();
        dragPawn = null;
        panning = false;
        connectorClick = false;
    }

    void StartConnect(UtilityScriptForConnectorFromTechArtist from)
    {
        connectFrom = from;
        connecting = true;
        dragConnect = false;
        foreach (var c in FindObjectsByType<UtilityScriptForConnectorFromTechArtist>())
            if (c && c.Pawn != from.Pawn) c.SetHighlighted(true);
    }

    void EndConnect(UtilityScriptForConnectorFromTechArtist to)
    {
        foreach (var c in FindObjectsByType<UtilityScriptForConnectorFromTechArtist>())
            if (c) c.SetHighlighted(false);

        if (connectFrom && to && connectFrom.Pawn != to.Pawn && !Linked(connectFrom, to))
            AddLink(connectFrom, to);

        connectFrom = null;
        connecting = false;
        dragConnect = false;
    }

    bool Linked(UtilityScriptForConnectorFromTechArtist a, UtilityScriptForConnectorFromTechArtist b)
    {
        foreach (var (la, lb, _) in links)
            if ((la == a && lb == b) || (la == b && lb == a)) return true;
        return false;
    }

    void AddLink(UtilityScriptForConnectorFromTechArtist a, UtilityScriptForConnectorFromTechArtist b)
    {
        var line = new GameObject("Link").AddComponent<LineRenderer>();
        line.sharedMaterial = linkMaterial;
        line.startWidth = line.endWidth = 0.07f;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.SetPosition(0, a.transform.position);
        line.SetPosition(1, b.transform.position);
        links.Add((a, b, line));
    }

    public void OnPawnDestroyed(UnilityScriptForPawnFormTechArtists pawn)
    {
        var dead = new HashSet<UtilityScriptForConnectorFromTechArtist>(pawn.GetComponentsInChildren<UtilityScriptForConnectorFromTechArtist>());
        for (int i = links.Count - 1; i >= 0; i--)
        {
            if (!dead.Contains(links[i].a) && !dead.Contains(links[i].b)) continue;
            Destroy(links[i].line.gameObject);
            links.RemoveAt(i);
        }
        if (dragPawn == pawn) dragPawn = null;
        if (connectFrom && dead.Contains(connectFrom)) EndConnect(null);
    }

    Collider UnderMouse()
    {
        return Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit) ? hit.collider : null;
    }

    Vector3 GroundUnderMouse(Vector3 camPos)
    {
        var m = Input.mousePosition;
        var near = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, cam.nearClipPlane));
        var far = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, cam.farClipPlane));
        var dir = far - near;
        if (Mathf.Abs(dir.y) < 1e-5f) return new Vector3(camPos.x, 0f, camPos.z);
        return camPos + dir * (-camPos.y / dir.y);
    }

    void ApplyZoom()
    {
        if (Mathf.Abs(zoomImpulse) < 0.001f)
        {
            zoomImpulse = 0f;
            return;
        }

        var vp = cam.ScreenToViewportPoint(Input.mousePosition);
        var dir = cam.transform.forward;
        dir += cam.transform.right * (vp.x - 0.5f) * 2f;
        dir += cam.transform.up * (vp.y - 0.5f) * 2f;

        cam.transform.position += dir.normalized * (zoomImpulse * ScrollSpeed * Time.deltaTime * 12f);
        zoomImpulse = Mathf.Lerp(zoomImpulse, 0f, Time.deltaTime * ZoomSmoothing);
    }
}
