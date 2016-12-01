using UnityEngine;

public class Botao : MonoBehaviour
{
	public GameObject botaoPosterior;
	internal GameObject botaoAnterior;
	internal bool selecionado;

	private Animator animator;
	private ControladorCena controladorCena;
	private Menu menu;

	void Start ()
	{
		animator = GetComponent<Animator>();
		controladorCena = ControladorCena.Pegar();
		menu = Menu.Pegar();

		GameObject[] botoes = GameObject.FindGameObjectsWithTag("Botão");

		foreach(GameObject botao in botoes)
		{
			Botao botaoScript = botao.GetComponent<Botao>();
			if (botaoScript.botaoPosterior == gameObject)
			{
				botaoAnterior = botao;
				break;
			}
		}
	}

	void Update()
	{
		AtualizarAnimacao();
	}

	void OnMouseEnter()
	{
		if (menu.habilitarSelecaoViaMouse)
			AlterarSelecao(true);
	}

	void OnMouseOver()
	{
		if (Input.GetMouseButtonDown(0))
			PressionarBotao();
	}

	void OnMouseExit()
	{
		if (menu.habilitarDesselecaoViaMouse)
			AlterarSelecao(false);
	}

	void AlterarSelecao(bool novaSelecao)
	{
		selecionado = novaSelecao;

		AtualizarAnimacao();
	}

	void AtualizarAnimacao()
	{
		animator.SetBool("selecionado", selecionado);
	}

	public void PressionarBotao()
	{
		controladorCena.menuLiberado = false;

		switch (gameObject.name)
		{
			case "Novo Jogo":
				controladorCena.NovoJogo();
				break;
			case "Tutorial":
				controladorCena.Tutorial();
				break;
			case "Sair":
				controladorCena.Sair();
				break;
		}
	}
}
