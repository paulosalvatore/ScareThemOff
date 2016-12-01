using UnityEngine;
using XInputDotNetPure;

public class Menu : MonoBehaviour
{
	[Header("Botões")]
	public GameObject botaoPadrao;
	public float botaoDelay = 0.2f;

	[Header("Seleção")]
	public bool habilitarSelecaoViaMouse;
	public bool habilitarDesselecaoViaMouse;

	[Header("Sons")]
	public AudioClip somSelecionarBotao;
	public AudioClip somPressionarBotao;

	private Botao botaoSelecionado;
	private float tempoUltimoBotao;
	private bool proximoBotaoLiberado = false;

	private ControladorCena controladorCena;
	private AudioSource audioSource;

	void Start ()
	{
		controladorCena = ControladorCena.Pegar();
		audioSource = GetComponent<AudioSource>();

		SelecionarBotao(botaoPadrao);
	}

	void Update ()
	{
		if (controladorCena.jogoPausado || !controladorCena.menuLiberado)
			return;

		float direcao = Input.GetAxisRaw("Vertical");
		GamePadState state = controladorCena.state;

		bool botaoUp = 
			(state.DPad.Up == ButtonState.Pressed) ||
			(state.ThumbSticks.Left.Y > 0.3f) ||
			(state.ThumbSticks.Right.Y > 0.3f);

		bool botaoDown =
			(state.DPad.Down == ButtonState.Pressed) ||
			(state.ThumbSticks.Left.Y < -0.3f) ||
			(state.ThumbSticks.Right.Y < -0.3f);
		
		if (!botaoUp && !botaoDown)
			proximoBotaoLiberado = true;
		else if (proximoBotaoLiberado || Time.time > tempoUltimoBotao)
		{
			if (botaoUp)
				SelecionarBotao(botaoSelecionado.botaoAnterior);
			else if (botaoDown)
				SelecionarBotao(botaoSelecionado.botaoPosterior);

			tempoUltimoBotao = Time.time + botaoDelay;
			proximoBotaoLiberado = false;
		}

		if (Input.GetButtonDown("IniciarInteraçãoObjeto"))
			PressionarBotao();
	}

	public void SelecionarBotao(GameObject botao)
	{
		if (botaoSelecionado)
		{
			botaoSelecionado.selecionado = false;
			audioSource.clip = somSelecionarBotao;
			audioSource.Play();
		}

		botaoSelecionado = botao.GetComponent<Botao>();
		botaoSelecionado.selecionado = true;
	}

	void PressionarBotao()
	{
		audioSource.clip = somPressionarBotao;
		audioSource.Play();

		botaoSelecionado.PressionarBotao();
	}

	static public Menu Pegar()
	{
		return GameObject.Find("Menu").GetComponent<Menu>();
	}
}
