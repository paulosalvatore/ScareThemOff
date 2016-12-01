using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tutorial : MonoBehaviour
{
	public List<Sprite> imagens = new List<Sprite>();
	private int imagemAtual = 0;
	private ControladorCena controladorCena;
	private SpriteRenderer spriteRenderer;

	void Start ()
	{
		controladorCena = ControladorCena.Pegar();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	void Update ()
	{
		if (Input.anyKeyDown)
			ProximaImagem();
	}

	void ProximaImagem()
	{
		imagemAtual++;
		if (imagemAtual < imagens.Count)
			spriteRenderer.sprite = imagens[imagemAtual];
		else
			controladorCena.CarregarCena("início");
	}
}
