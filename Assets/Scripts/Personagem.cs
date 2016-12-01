using UnityEngine;
using System.Collections;

public class Personagem : MonoBehaviour
{
	private SpriteRenderer spriteRenderer;

	internal GameObject comodoAtual;
	internal MeshRenderer luzComodoAtual;

	private new Rigidbody2D rigidbody;
	
	void Start()
	{
		rigidbody = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	public static GameObject PegarObjeto(string nomeObjeto, bool npc = false)
	{
		GameObject objeto = GameObject.Find(nomeObjeto);

		if (npc)
			objeto = objeto.transform.FindChild("NPC").gameObject;

		return objeto;
	}

	public static Personagem PegarControlador(string nomeObjeto, bool npc = false)
	{
		return PegarObjeto(nomeObjeto, npc).GetComponent<Personagem>();
	}

	public GameObject PegarComodoAtual()
	{
		return comodoAtual;
	}

	public void DefinirComodoAtual(GameObject comodo)
	{
		if (comodo != comodoAtual)
		{
			if (comodoAtual && gameObject.CompareTag("NPC"))
				comodoAtual.GetComponent<Comodo>().npcsComodo.Remove(transform.parent.gameObject);

			comodoAtual = comodo;

			if (comodoAtual)
			{
				if (gameObject.CompareTag("NPC"))
					comodoAtual.GetComponent<Comodo>().npcsComodo.Add(transform.parent.gameObject);

				luzComodoAtual = comodoAtual.GetComponent<Comodo>().PegarLuz();
			}
			else
				luzComodoAtual = null;
		}
	}
}
