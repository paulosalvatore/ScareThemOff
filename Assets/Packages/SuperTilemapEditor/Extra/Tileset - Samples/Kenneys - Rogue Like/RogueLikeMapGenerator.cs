using UnityEngine;
using System.Collections;

namespace CreativeSpore.SuperTilemapEditor
{
    public class RogueLikeMapGenerator : MonoBehaviour 
    {

        public Tilemap Ground;
        public Tilemap GroundOverlay;

        public int Width = 300;
        public int Height = 300;

        void OnGUI()
        {
            if(GUI.Button(new Rect(20, 20, 100, 50), "Generate Map"))
            {
                GenerateMap();
            }
        }

        [ContextMenu("GenerateMap")]
	    public void GenerateMap()
        {
            Ground.ClearMap();
            GroundOverlay.ClearMap();

            float now;
            now = Time.realtimeSinceStartup;
            float fDiv = 25f;
            float xf = Random.value * 100;
            float yf = Random.value * 100;

            //*/ Rogue Demo (280ms with 180x180)
            uint tileWater = (8 << 16);
            uint tileWaterPlants = (24 << 16);
            uint tileDarkGrass = 66;
            uint tileGrass = (9 << 16);
            uint tileFlowers = (22 << 16);
            uint tileMountains = (23 << 16);
            //*/

            // Oryx Demo
            /*/
            uint tileWater = (9 << 16);
            uint tileWaterPlants = (16 << 16);
            uint tileDarkGrass = 757;
            uint tileGrass = 756;
            uint tileFlowers = (17 << 16);
            uint tileMountains = (18 << 16);
            //*/

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    float fRand = Random.value;
                    float noise = Mathf.PerlinNoise((i + xf) / fDiv, (j + yf) / fDiv);
                    //Debug.Log( "noise: "+noise+"; i: "+i+"; j: "+j );
                    if (noise < 0.3) //water
                    {
                        Ground.SetTileData(i, j, tileWater);
                    }
                    else if (noise < 0.4) // water plants
                    {
                        Ground.SetTileData(i, j, tileWater);
                        if (fRand < noise / 3)
                            GroundOverlay.SetTileData(i, j, tileWaterPlants);
                    }
                    else if (noise < 0.5 && fRand < (1 - noise / 2)) // dark grass
                    {
                        Ground.SetTileData(i, j, tileDarkGrass);
                    }
                    else if (noise < 0.6 && fRand < (1 - 1.2 * noise)) // flowers
                    {
                        Ground.SetTileData(i, j, tileGrass);
                        GroundOverlay.SetTileData(i, j, tileFlowers);
                    }
                    else if (noise < 0.7) // grass
                    {
                        Ground.SetTileData(i, j, tileGrass);
                    }
                    else // mountains
                    {
                        Ground.SetTileData(i, j, tileGrass);
                        GroundOverlay.SetTileData(i, j, tileMountains);
                    }
                }
            }
            Debug.Log("Generation time(ms): " + (Time.realtimeSinceStartup - now) * 1000);

            now = Time.realtimeSinceStartup;
            Ground.UpdateMesh();
            GroundOverlay.UpdateMesh();
            Debug.Log("UpdateMesh time(ms): " + (Time.realtimeSinceStartup - now) * 1000);
        }
    }
}
