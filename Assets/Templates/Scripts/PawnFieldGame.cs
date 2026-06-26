using System.Collections.Generic;
using CrazyPawn;
using UnityEngine;

public class PawnFieldGame : MonoBehaviour
{
    public static PawnFieldGame Instance { get; private set; }

    public CrazyPawnSettings settings;
    [SerializeField] GameObject pawnPrefab;
    [SerializeField] GameObject cellPrefab;
    [SerializeField] Material linkMaterial;

    const float CellSize = 1.5f;
    const float ScrollSpeed = 2f;
    const float ZoomSmoothing = 12f;
    const float DragThresholdSq = 25f;

    float boardHalf;
    Camera cam;
    Material cellTemplate, boardBlack, boardWhite;

    UnilityScriptForPawnFormTechArtists dragPawn;
    bool panning, connecting, dragConnect, connectorClick;
    Vector3 panGround, panCam;
    Vector2 mouseDown;
    UtilityScriptForConnectorFromTechArtist connectFrom;
    float zoomImpulse;

    readonly List<UnilityScriptForPawnFormTechArtists> pawns = new();
    readonly List<(UtilityScriptForConnectorFromTechArtist a, UtilityScriptForConnectorFromTechArtist b, LineRenderer line)> links = new();

    public bool IsOffBoard(Vector3 p) => Mathf.Abs(p.x) > boardHalf || Mathf.Abs(p.z) > boardHalf;

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
        cellTemplate = cellPrefab.GetComponent<Renderer>().sharedMaterial;
        boardHalf = settings.CheckerboardSize * CellSize * 0.5f;
        CreateBoard();
        SpawnPawns();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (boardBlack) Destroy(boardBlack);
        if (boardWhite) Destroy(boardWhite);
        for (int i = links.Count - 1; i >= 0; i--) RemoveLinkAt(i);
        pawns.Clear();
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
            if (!a || !b) { RemoveLinkAt(i); continue; }
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
        {
            for (int z = 0; z < settings.CheckerboardSize; z++)
            {
                var cell = Instantiate(cellPrefab, root.transform);
                cell.transform.position = new Vector3(-origin + x * CellSize, 0f, -origin + z * CellSize);
                cell.GetComponent<Renderer>().sharedMaterial = (x + z) % 2 == 0 ? boardBlack : boardWhite;
            }
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
            var p = Random.insideUnitCircle * settings.InitialZoneRadius;
            var pawn = Instantiate(pawnPrefab, new Vector3(p.x, 0f, p.y), Quaternion.identity)
                .GetComponent<UnilityScriptForPawnFormTechArtists>();
            pawn.Init();
            pawns.Add(pawn);
        }
    }

    void MouseDown()
    {
        mouseDown = Input.mousePosition;
        connectorClick = false;

        if (UnderMouse(out var col))
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
        if (connectorClick && connecting && ((Vector2)Input.mousePosition - mouseDown).sqrMagnitude > DragThresholdSq)
            dragConnect = true;
        if (dragPawn) dragPawn.Drag(GroundUnderMouse(cam.transform.position));
        if (!panning) return;
        var delta = panGround - GroundUnderMouse(panCam);
        cam.transform.position = panCam + new Vector3(delta.x, 0f, delta.z);
    }

    void MouseUp()
    {
        if (dragConnect && connecting)
            EndConnect(UnderMouse(out var col) ? col.GetComponent<UtilityScriptForConnectorFromTechArtist>() : null);
        dragPawn?.EndDrag();
        dragPawn = null;
        panning = connectorClick = false;
    }

    void StartConnect(UtilityScriptForConnectorFromTechArtist from)
    {
        connectFrom = from;
        connecting = true;
        dragConnect = false;
        from.SetWaitingConnect(true);
        SetPawnHighlights(true, from.Pawn);
    }

    void EndConnect(UtilityScriptForConnectorFromTechArtist to)
    {
        if (connecting)
        {
            if (connectFrom) connectFrom.SetWaitingConnect(false);
            SetPawnHighlights(false);
        }
        var from = connectFrom;
        connectFrom = null;
        connecting = dragConnect = false;
        if (from && to && from.Pawn != to.Pawn && !HasLink(from, to)) AddLink(from, to);
    }

    void SetPawnHighlights(bool on, UnilityScriptForPawnFormTechArtists skip = null)
    {
        for (int i = 0; i < pawns.Count; i++)
            if (pawns[i] && pawns[i] != skip) pawns[i].SetConnectorsHighlighted(on);
    }

    bool HasLink(UtilityScriptForConnectorFromTechArtist a, UtilityScriptForConnectorFromTechArtist b)
    {
        for (int i = 0; i < links.Count; i++)
        {
            var (la, lb, _) = links[i];
            if ((la == a && lb == b) || (la == b && lb == a)) return true;
        }
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

    void RemoveLinkAt(int index)
    {
        if (links[index].line) Destroy(links[index].line.gameObject);
        links.RemoveAt(index);
    }

    public void OnPawnDestroyed(UnilityScriptForPawnFormTechArtists pawn)
    {
        pawns.Remove(pawn);
        for (int i = links.Count - 1; i >= 0; i--)
        {
            var (a, b, _) = links[i];
            if (pawn.OwnsConnector(a) || pawn.OwnsConnector(b)) RemoveLinkAt(i);
        }
        if (dragPawn == pawn) dragPawn = null;
        if (connectFrom && pawn.OwnsConnector(connectFrom)) EndConnect(null);
    }

    bool UnderMouse(out Collider col)
    {
        if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit))
        {
            col = hit.collider;
            return true;
        }
        col = null;
        return false;
    }

    Vector3 GroundUnderMouse(Vector3 camPos)
    {
        var m = Input.mousePosition;
        var near = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, cam.nearClipPlane));
        var dir = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, cam.farClipPlane)) - near;
        if (Mathf.Abs(dir.y) < 1e-5f) return new Vector3(camPos.x, 0f, camPos.z);
        return camPos + dir * (-camPos.y / dir.y);
    }

    void ApplyZoom()
    {
        if (Mathf.Abs(zoomImpulse) < 0.001f) { zoomImpulse = 0f; return; }
        var vp = cam.ScreenToViewportPoint(Input.mousePosition);
        var dir = cam.transform.forward;
        dir += cam.transform.right * (vp.x - 0.5f) * 2f;
        dir += cam.transform.up * (vp.y - 0.5f) * 2f;
        cam.transform.position += dir.normalized * (zoomImpulse * ScrollSpeed * Time.deltaTime * 12f);
        zoomImpulse = Mathf.Lerp(zoomImpulse, 0f, Time.deltaTime * ZoomSmoothing);
    }
}
