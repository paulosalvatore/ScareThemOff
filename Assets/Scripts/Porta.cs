using UnityEngine;
using System.Collections;

public class Porta : MonoBehaviour
{
	private bool aberta = false;
	private GameObject personagem;

	private Animator animator;

	void Start()
	{
		animator = GetComponent<Animator>();
	}

	void OnTriggerEnter2D(Collider2D coll)
	{
		if (coll.CompareTag("NPC"))
		{
			personagem = coll.gameObject;
			animator.SetBool("abrir", true);
		}
	}

	void OnTriggerExit2D(Collider2D coll)
	{
		if ((coll.CompareTag("NPC")) && personagem == coll.gameObject)
		{
			animator.SetBool("abrir", false);
		}
	}
}
