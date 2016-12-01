using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{
    [DisallowMultipleComponent]
    public class TilemapGroup : MonoBehaviour 
    {
        public Tilemap SelectedTilemap { get { return m_selectedIndex >= 0 && m_selectedIndex < m_tilemaps.Count ? m_tilemaps[m_selectedIndex] : null; } }
        public IList<Tilemap> Tilemaps { get { return m_tilemaps.AsReadOnly(); } }

        [SerializeField]
        private List<Tilemap> m_tilemaps;
        [SerializeField]
        private int m_selectedIndex = -1;

        void OnValidate()
        {
            if (Tilemaps.Count != transform.childCount)
            {
                Refresh();
            }
        }

	    void Start () 
        {
            Refresh();
	    }

        void OnDrawGizmosSelected()
        {
            if(SelectedTilemap)
            {
                SelectedTilemap.SendMessage("DoDrawGizmos");
            }
        }
	    
        public void Refresh()
        {
            m_tilemaps = new List<Tilemap>( GetComponentsInChildren<Tilemap>() );
            m_selectedIndex = Mathf.Clamp(m_selectedIndex, -1, m_tilemaps.Count);
        }
    }
}