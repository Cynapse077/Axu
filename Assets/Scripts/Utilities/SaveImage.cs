using UnityEngine;
using System.Collections;
using System.IO;

public static class SaveImage {
	
	public static void Save(Texture2D tex) {
		Texture2D savedTexture = tex as Texture2D;
		Texture2D newTexture = new Texture2D(savedTexture.width, savedTexture.height, TextureFormat.ARGB32, false);
		
		newTexture.SetPixels(0,0, savedTexture.width, savedTexture.height, savedTexture.GetPixels());
		newTexture.Apply();
		byte[] bytes = newTexture.EncodeToPNG();
		string path = Application.persistentDataPath + "/Axu Overworld Map.png";
		File.WriteAllBytes(path, bytes);
	}    
}
