using UnityEngine;
using System.Collections;

public class FecharAplicacao : MonoBehaviour
{
	static public void Sair()
	{
		#if UNITY_STANDALONE
			Application.Quit();
		#endif

		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#endif
	}
}
