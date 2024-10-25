
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XiaoCaoEditor
{
    public class SpriteAssetPostprocessor : AssetPostprocessor
    {
        // 指定目录路径，相对于Assets目录  
        private static readonly string targetDirectoryPath = "Assets/_Res/Sprite";

        // 当纹理文件被导入时调用  
        private void OnPreprocessTexture()
        {
            Debug.Log($"--- assetPath {assetPath}");
            // 检查文件是否在目标目录中  
            if (assetPath.StartsWith(targetDirectoryPath))
            {
                // 获取纹理导入设置  
                TextureImporter textureImporter = (TextureImporter)assetImporter;

                // 设置纹理类型为Sprite (2D and UI)  
                textureImporter.textureType = TextureImporterType.Sprite;

                // 可选：设置Sprite导入模式（例如，单个精灵、多精灵等）  
                // textureImporter.spriteImportMode = SpriteImportMode.Single;  

                textureImporter.spritePixelsPerUnit = 100; // 根据需要调整  
                textureImporter.mipmapEnabled = false; // 禁用mipmap  

                textureImporter.SaveAndReimport();
            }
        }
    }


}
