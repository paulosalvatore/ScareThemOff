using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XInputDotNetPure;
using UnityEngine.SceneManagement;

public class Intro2 : MonoBehaviour
{
	ControladorCena controladorCena;

	[Header("Ônibus")]
	public bool onibus;
	public Vector3 posicaoOnibus1;
	private float duracaoMovimentoOnibus1;
	public Vector3 posicaoOnibus2;
	private float duracaoMovimentoOnibus2;
	public float duracaoOnibusParado;
	public List<AudioClip> audiosOnibus = new List<AudioClip>();

	[Header("Porta")]
	public bool porta;
	public Vector3 posicaoPorta;
	public float duracaoMovimentoPorta;
	public float delayPorta;

	[Header("Turistas")]
	public bool turistas;
	public float delayInicialTuristas;
	public float delayMovimentacaoTuristas;
	public float duracaoMovimentacaoTuristas;
	public Vector3 posicaoTuristas;
	public Vector3 variacaoTuristas;

	private AudioSource audioMovimento;
	private AudioSource audioFreio;
	private AudioSource audioParado;

	private int vibrandoId = 0;

	void Start ()
	{
		controladorCena = ControladorCena.Pegar();

		if (onibus)
		{
			duracaoMovimentoOnibus1 = Vector3.Distance(transform.position, posicaoOnibus1) / 5;
			iTween.MoveTo(gameObject, iTween.Hash("position", posicaoOnibus1, "easeType", iTween.EaseType.linear, "time", duracaoMovimentoOnibus1));
			StartCoroutine(VibrarOnibus(0.3f, duracaoMovimentoOnibus1));
			StartCoroutine(VibrarOnibus(0.1f, duracaoOnibusParado, duracaoMovimentoOnibus1));
			StartCoroutine(MovimentarOnibus2());
			
			audioMovimento = AdicionarAudioSourceOnibus(audiosOnibus[0]);
			audioFreio = AdicionarAudioSourceOnibus(audiosOnibus[1], 0.45f);
			audioParado = AdicionarAudioSourceOnibus(audiosOnibus[2], 0.15f);

			audioMovimento.Play();
			StartCoroutine(TocarAudioDelay(audioFreio, duracaoMovimentoOnibus1 - 0.2f));
			StartCoroutine(TocarAudioDelay(audioParado, duracaoMovimentoOnibus1));
		}
		else if (porta)
			StartCoroutine(Porta());
		else if (turistas)
			StartCoroutine(Turistas());
	}

	void Update()
	{
		if (controladorCena.state.Buttons.Start == ButtonState.Pressed)
			controladorCena.CarregarCena("jogo");
	}

	IEnumerator VibrarOnibus(float intensidade, float duracao, float delayInicial = 0)
	{
		vibrandoId += 1;
		int vibrando = vibrandoId;
		yield return new WaitForSeconds(delayInicial);
		GamePad.SetVibration(controladorCena.playerIndex, intensidade, intensidade);
		yield return new WaitForSeconds(duracao);
		if (vibrando == vibrandoId)
			GamePad.SetVibration(controladorCena.playerIndex, 0, 0);
	}

	IEnumerator MovimentarOnibus2()
	{
		yield return new WaitForSeconds(duracaoMovimentoOnibus1 + duracaoOnibusParado - 0.4f);
		audioMovimento.Play();
		yield return new WaitForSeconds(0.4f);
		duracaoMovimentoOnibus2 = Vector3.Distance(transform.position, posicaoOnibus2) / 5;
		StartCoroutine(VibrarOnibus(0.3f, duracaoMovimentoOnibus2));
		iTween.MoveTo(gameObject, iTween.Hash("position", posicaoOnibus2, "easeType", iTween.EaseType.linear, "time", duracaoMovimentoOnibus2));
	}

	AudioSource AdicionarAudioSourceOnibus(AudioClip clip, float volume = 1f)
	{
		AudioSource audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
		audioSource.clip = clip;
		audioSource.volume = volume;

		return audioSource;
	}

	IEnumerator TocarAudioDelay(AudioSource audio, float delay)
	{
		yield return new WaitForSeconds(delay);
		audio.Play();
	}

	IEnumerator Porta ()
	{
		yield return new WaitForSeconds(delayPorta);
		iTween.MoveTo(gameObject, iTween.Hash("position", posicaoPorta, "easeType", iTween.EaseType.linear, "time", duracaoMovimentoPorta));
	}

	IEnumerator Turistas()
	{
		yield return new WaitForSeconds(delayInicialTuristas);

		foreach (Transform turista in transform)
			turista.gameObject.SetActive(true);
		
		yield return new WaitForSeconds(delayMovimentacaoTuristas);

		foreach (Transform turista in transform)
		{
			Vector3 posicaoTurista = new Vector3(
				posicaoTuristas.x + Random.Range(-variacaoTuristas.x, variacaoTuristas.x),
				posicaoTuristas.y + Random.Range(-variacaoTuristas.y, variacaoTuristas.y),
				posicaoTuristas.z
			);

			iTween.MoveTo(turista.gameObject, iTween.Hash("position", posicaoTurista, "easeType", iTween.EaseType.linear, "time", duracaoMovimentacaoTuristas));
		}
		
		yield return new WaitForSeconds(duracaoMovimentacaoTuristas - 0.8f);
		
		foreach (Transform turista in transform)
			StartCoroutine(AplicarFade(turista.GetComponent<SpriteRenderer>()));

		StartCoroutine(CarregarIntro2());
	}

	IEnumerator AplicarFade(SpriteRenderer spriteRenderer)
	{
		float alpha = spriteRenderer.color.a;
		float intervalo = 0.01f;

		while (alpha > 0)
		{
			alpha -= intervalo;
			spriteRenderer.color = new Vector4(1, 1, 1, alpha);

			yield return new WaitForSeconds(0.01f);
		}
	}

	IEnumerator CarregarIntro2()
	{
		yield return new WaitForSeconds(1.3f);
		controladorCena.CarregarCena("jogo");
	}
}
