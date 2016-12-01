using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using XInputDotNetPure;

public class ControladorCena : MonoBehaviour
{
	[Header("Fade")]
	public float duracaoFade;
	public float fpsFade;
	private SpriteRenderer fade;

	[Header("Delay após carregar a cena")]
	public float delayCenaCarregada;
	internal bool trocaCenaLiberada = true;
	private bool carregarCenaIniciado;

	[Header("Controle")]
	public bool exibirInformacoesControle;
	internal bool playerIndexSet = false;
	internal PlayerIndex playerIndex;
	internal GamePadState state;
	internal GamePadState prevState;

	[Header("Sons - Background")]
	public List<AudioClip> sonsCenas;

	[Header("Componentes de Áudio")]
	public float duracaoFadeSom;
	public AudioMixerSnapshot volumeDown;
	public AudioMixerSnapshot volumeUp;

	internal bool menuLiberado;

	private Pausar pausar;
	private AudioSource audioSource;

	internal bool jogoPausado;

	internal int cenaAtiva;

	void Start()
	{
		GameObject objetoFade = GameObject.Find("Fade");
		if (objetoFade)
			fade = objetoFade.GetComponent<SpriteRenderer>();

		pausar = GetComponent<Pausar>();
		audioSource = GetComponent<AudioSource>();
		
		if (SceneManager.GetActiveScene().buildIndex == 0)
			CarregarCena("início");
	}

	void Update()
	{
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;

		cenaAtiva = SceneManager.GetActiveScene().buildIndex;

		if (!playerIndexSet || !prevState.IsConnected)
		{
			for (int i = 0; i < 4; ++i)
			{
				PlayerIndex checkPlayerIndex = (PlayerIndex)i;
				GamePadState checkState = GamePad.GetState(checkPlayerIndex);
				if (checkState.IsConnected)
				{
					playerIndex = checkPlayerIndex;
					playerIndexSet = true;
				}
			}
		}

		prevState = state;
		state = GamePad.GetState(playerIndex);
	}

	public void CarregarCena(string cena)
	{
		if (jogoPausado)
			pausar.DespausarJogo();

		GamePad.SetVibration(playerIndex, 0, 0);
		int cenaId = 0;
		bool carregarCena = true;

		switch (cena)
		{
			case "início":
				cenaId = 0;
				break;
			case "jogo":
				cenaId = 1;
				break;
			case "gameover":
				cenaId = 2;
				break;
			case "intro1":
				cenaId = 3;
				break;
			case "intro2":
				cenaId = 4;
				break;
			case "tutorial":
				cenaId = 5;
				break;
			default:
				carregarCena = false;
				break;
		}
		
		if (carregarCena)
			StartCoroutine(CarregarCena(cenaId));
	}

	IEnumerator LiberarMenuAposFade()
	{
		while (fade.color.a != 0f)
			yield return new WaitForSeconds(delayCenaCarregada);

		menuLiberado = true;
	}

	IEnumerator LiberarTrocaCenaAposFade()
	{
		while (fade.color.a != 0f)
			yield return new WaitForSeconds(delayCenaCarregada);

		trocaCenaLiberada = true;
		carregarCenaIniciado = false;
	}

	IEnumerator CarregarCena(int cena)
	{
		if (!carregarCenaIniciado)
		{
			carregarCenaIniciado = true;
			while (!trocaCenaLiberada)
				yield return new WaitForSeconds(delayCenaCarregada);

			trocaCenaLiberada = false;

			float delay = delayCenaCarregada;
			bool cenaDiferente = cenaAtiva != cena;
		
			if (cenaDiferente)
			{
				volumeDown.TransitionTo(duracaoFadeSom);

				StartCoroutine(FadeScene());

				delay += duracaoFade;
			}
			else
				fade.color = new Vector4(0, 0, 0, 1f);

			yield return new WaitForSeconds(delay);

			if (cenaDiferente)
				SceneManager.LoadScene(cena);

			StartCoroutine(FadeScene(delayCenaCarregada));
			
			if (sonsCenas[cena])
			{
				audioSource.clip = sonsCenas[cena];
				audioSource.Play();
				volumeUp.TransitionTo(duracaoFadeSom);
			}
			
			if (cena == 0f)
				StartCoroutine(LiberarMenuAposFade());

			StartCoroutine(LiberarTrocaCenaAposFade());
		}
	}

	public void NovoJogo()
	{
		//CarregarCena("jogo");
		CarregarCena("intro1");
	}

	public void Tutorial()
	{
		CarregarCena("tutorial");
	}

	public void Sair()
	{
		#if UNITY_STANDALONE
			Application.Quit();
		#endif

		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#endif
	}

	IEnumerator FadeScene(float delay)
	{
		yield return new WaitForSeconds(delay);
		StartCoroutine(FadeScene());
	}

	IEnumerator FadeScene()
	{
		float fadeType = (fade.color.a == 0 ? 1 : -1);
		
		while (true)
		{
			float alpha = Mathf.Clamp(fade.color.a + 1 / fpsFade * fadeType, 0f, 1f);
			fade.color = new Vector4(0, 0, 0, alpha);

			if (fade.color.a == 0f || fade.color.a == 1f)
				break;

			yield return new WaitForSeconds(duracaoFade / fpsFade);
		}
	}

	static public ControladorCena Pegar()
	{
		return GameObject.Find("ControladorCena").GetComponent<ControladorCena>();
	}

	void OnGUI()
	{
		if (exibirInformacoesControle)
		{
			string text = string.Format("IsConnected {0} Packet #{1}\n", state.IsConnected, state.PacketNumber);
			text += string.Format("\tTriggers {0} {1}\n", state.Triggers.Left, state.Triggers.Right);
			text += string.Format("\tD-Pad {0} {1} {2} {3}\n", state.DPad.Up, state.DPad.Right, state.DPad.Down, state.DPad.Left);
			text += string.Format("\tButtons Start {0} Back {1} Guide {2}\n", state.Buttons.Start, state.Buttons.Back, state.Buttons.Guide);
			text += string.Format("\tButtons LeftStick {0} RightStick {1} LeftShoulder {2} RightShoulder {3}\n", state.Buttons.LeftStick, state.Buttons.RightStick, state.Buttons.LeftShoulder, state.Buttons.RightShoulder);
			text += string.Format("\tButtons A {0} B {1} X {2} Y {3}\n", state.Buttons.A, state.Buttons.B, state.Buttons.X, state.Buttons.Y);
			text += string.Format("\tSticks Left {0} {1} Right {2} {3}\n", state.ThumbSticks.Left.X, state.ThumbSticks.Left.Y, state.ThumbSticks.Right.X, state.ThumbSticks.Right.Y);
			GUI.Label(new Rect(0, 0, Screen.width, Screen.height), text);
		}
	}
}
