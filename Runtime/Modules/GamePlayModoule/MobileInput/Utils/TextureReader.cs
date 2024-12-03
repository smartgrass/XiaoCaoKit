using UnityEngine;

namespace MFPC.Utils
{
    public class TextureReader
    {
        /// <summary>
        /// Find texture by raycast
        /// </summary>
        /// <param name="raycastHit"></param>
        /// <returns>Founded texture</returns>
        public Texture GetTextureFromRaycast(RaycastHit raycastHit)
        {
            Texture rendererTexture = GetTextureFromRenderer(raycastHit);
            return rendererTexture == null ? GetTextureFromTerrain(raycastHit) : rendererTexture;
        }

        private Texture GetTextureFromRenderer(RaycastHit raycastHit)
        {
            if (raycastHit.collider.TryGetComponent<Renderer>(out Renderer renderer) && renderer.material.mainTexture is Texture)
            {
                return renderer.material.mainTexture as Texture;
            }

            return null;
        }

        private Texture GetTextureFromTerrain(RaycastHit raycastHit)
        {
            if (raycastHit.collider.TryGetComponent<Terrain>(out Terrain terrain) && terrain.terrainData.terrainLayers.Length > 0)
            {
                Vector3 terrainLocalPos = raycastHit.point - terrain.transform.position;
                float[] textureValues = GetTextureValues(terrainLocalPos, terrain.terrainData);

                float maxTextureValue = 0f;
                int maxTextureIndex = 0;
                for (int i = 0; i < textureValues.Length; i++)
                {
                    if (textureValues[i] > maxTextureValue)
                    {
                        maxTextureValue = textureValues[i];
                        maxTextureIndex = i;
                    }
                }

                return terrain.terrainData.terrainLayers[maxTextureIndex].diffuseTexture;
            }

            return null;
        }

        private float[] GetTextureValues(Vector3 terrainLocalPos, TerrainData terrainData)
        {
            Vector3 normalizedPos = new Vector3(terrainLocalPos.x / terrainData.size.x, 0, terrainLocalPos.z / terrainData.size.z);
            int mapSize = terrainData.alphamapResolution;
            int mapX = Mathf.RoundToInt(normalizedPos.x * (mapSize - 1));
            int mapY = Mathf.RoundToInt(normalizedPos.z * (mapSize - 1));

            float[,,] alphaMaps = terrainData.GetAlphamaps(mapX, mapY, 1, 1);

            float[] textureValues = new float[alphaMaps.GetUpperBound(2) + 1];
            for (int i = 0; i < textureValues.Length; i++)
            {
                textureValues[i] = alphaMaps[0, 0, i];
            }

            return textureValues;
        }
    }
}