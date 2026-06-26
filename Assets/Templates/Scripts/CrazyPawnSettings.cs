using UnityEngine;

namespace CrazyPawn
{
    [CreateAssetMenu(menuName = "CrazyPawn/Settings", fileName = "CrazyPawnSettings")]
    public class CrazyPawnSettings : ScriptableObject
    {
        public float InitialZoneRadius = 10f;
        public int InitialPawnCount = 7;

        public Material DeleteMaterial;
        public Material ActiveConnectorMaterial;

        public int CheckerboardSize = 18;
        public Color BlackCellColor = Color.black;
        public Color WhiteCellColor = Color.white;
    }
}
