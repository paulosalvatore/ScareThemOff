using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XInputDotNetPure;

public static class EnumerableExtension
{
	public static T PickRandom<T>(this IEnumerable<T> source)
	{
		return source.PickRandom(1).Single();
	}

	public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
	{
		return source.Shuffle().Take(count);
	}

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
	{
		return source.OrderBy(x => Guid.NewGuid());
	}
}

public class DisplayWithoutEdit : PropertyAttribute
{
	public DisplayWithoutEdit() {}
}

[System.Serializable]
public class Delay
{
	public float min, max;
}

[System.Serializable]
public class NpcsSprites
{
	public List<Sprite> frente, costas;
}

[System.Serializable]
public class Luz
{
	public float apagada, acesa, oscilandoMin, oscilandoMax;
}

public class Jogo : MonoBehaviour
{
	[Header("Luz (0 = claro, 1 = escuro)")]
	public Luz luz;
	public float oscilarDuracao;
	public float delayAlterarLuz;
	public float delayChecarLuzComodo;
	public float delayComodoExorcisado;
	private float tempoAlterarLuz;

	[Header("Waypoint")]
	public Vector2 variacaoWaypoint;
	public GameObject waypointInicial;
	public GameObject waypointObjeto;
	internal int waypointId;

	[Header("Distância Máxima entre Personagens")]
	public float distanciaMaxima;

	private MeshRenderer luzOscilacao;
	private bool luzOscilando;

	private Jogador jogador;
	private Personagem personagem;
	internal Exorcista exorcista;
	private AudioSource audioSource;
	internal ControladorCena controladorCena;

	[Header("Almas")]
	[DisplayWithoutEdit()] public int almas;
	public int objetivo;
	public GameObject coletarAlmaFX;
	private Text almasText;
	public GameObject almaPrefab;
	private List<Animator> almasHUD = new List<Animator>();
	private List<Animator> vidasHUD = new List<Animator>();

	[Header("NPCs")]
	public GameObject npc;
	public int quantidadeNpcsInicial;
	public float chanceNpcAdicionalComodo;
	public float chanceNpcChamadaComodo;
	[DisplayWithoutEdit()] public int npcsEmJogo;
	[DisplayWithoutEdit()] public int quantidadeNpcsRestantes;
	[DisplayWithoutEdit()] public int quantidadeNpcsSairam;
	public NpcsSprites cabelos;
	public NpcsSprites camisas;
	public NpcsSprites bermudas;

	[Header("Delays")]
	public float delayInicioJogo;
	public Delay delayInicialNpcs;
	public Delay delayMovimentoNpcs;
	public Delay delayInteracaoObjetoNpcs;
	public Delay delayEntradaNpcs;
	public Delay delayChamadaNpcs;
	public Delay delayComodoChamarNpcs;

	[Header("Áudios")]
	public List<AudioClip> gritos;
	public AudioClip comandoDesativado;
	public AudioClip coracaoBatendo;
	public AudioClip audioVitoria;
	internal AudioSource audioSourceEfeitos;

	[Header("Objetos")]
	public GameObject bordaObjetos;
	public List<Sprite> spritesBordaObjetos;

	[Header("Delay Comandos")]
	public float delayComando;
	private float tempoUltimoComando;
	public float delayComandoLiberado;
	public float delayComandoDesativado;

	[Header("Controlador da Cena")]
	public GameObject controladorCenaPrefab;

	[Header("Câmera")]
	public Vector3 cameraPosicaoZoomOut;
	private Vector3 cameraPosicaoNormal;
	public Vector3 cameraPosicaoFinal;
	public float cameraSizeZoomOut;
	private float cameraSizeNormal;
	public float duracaoZoom;
	public float fpsZoom;
	internal new Camera camera;
	internal bool cameraTravada;
	private bool cameraZoomLiberado = true;
	private bool cameraBotaoZoomLiberado;
	private bool cameraLiberarComandosJogador;

	private GameObject botaoAuxilioParent;
	private Dictionary<string, BotaoAuxilio> botoesAuxilio = new Dictionary<string, BotaoAuxilio>();
	private GameObject botoesAuxilioTextosParent;
	private Dictionary<string, Text> botoesAuxilioTextos = new Dictionary<string, Text>();
	private bool alterarEstadoBotoesAuxilioLiberado;
	private int estadoBotoesAjuda = 2;

	private Text relogioText;
	private float tempoJogo;

	public GameObject telaVitoria;
	private bool anyKeyRestart;
	internal bool vitoria;

	public bool desenharLinecast;
	private GameObject npcLinecast;

	void Start()
	{
		tempoJogo = Time.time;

		audioSource = GetComponent<AudioSource>();
		jogador = Jogador.PegarControlador();
		personagem = Personagem.PegarControlador("Jogador");
		exorcista = Exorcista.PegarControlador();

		camera = GameObject.Find("Main Camera").GetComponent<Camera>();
		cameraSizeNormal = camera.GetComponent<Camera>().orthographicSize;
		cameraPosicaoZoomOut.z = camera.transform.position.z;
		AtualizarPosicaoCameraJogador();

		if (!GameObject.Find("ControladorCena"))
			Instantiate(controladorCenaPrefab).name = "ControladorCena";

		controladorCena = ControladorCena.Pegar();

		audioSourceEfeitos = gameObject.AddComponent<AudioSource>();
		audioSourceEfeitos.playOnAwake = false;
		audioSourceEfeitos.loop = true;

		quantidadeNpcsRestantes = quantidadeNpcsInicial;
		StartCoroutine(IniciarJogo());

		PegarAlmasHUD();
		PegarVidasHUD();

		relogioText = GameObject.Find("Relógio Text").GetComponent<Text>();

		botaoAuxilioParent = GameObject.Find("Botões Auxílio");

		foreach (Transform botaoAuxilio in botaoAuxilioParent.transform)
		{
			BotaoAuxilio botaoAuxilioScript = botaoAuxilio.GetComponent<BotaoAuxilio>();
			if (botaoAuxilioScript)
				botoesAuxilio.Add(botaoAuxilio.name, botaoAuxilioScript);
		}

		botoesAuxilioTextosParent = GameObject.Find("Textos");

		foreach (Transform textoBotaoAuxilio in botoesAuxilioTextosParent.transform)
			botoesAuxilioTextos.Add(textoBotaoAuxilio.name, textoBotaoAuxilio.GetComponent<Text>());

		AtualizarExibicaoBotoesAuxilio();
	}

	void PegarAlmasHUD()
	{
		foreach (Transform child in GameObject.Find("Almas").transform)
			almasHUD.Add(child.GetComponent<Animator>());
	}

	void PegarVidasHUD()
	{
		foreach (Transform child in GameObject.Find("Vidas").transform)
			vidasHUD.Add(child.GetComponent<Animator>());
	}

	void Update()
	{
		if (anyKeyRestart && Input.anyKeyDown)
			controladorCena.CarregarCena("início");

		if (Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown("joystick button 9"))
			ColetarAlma(jogador.gameObject);

		if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown("joystick button 6"))
			desenharLinecast = !desenharLinecast;

		if (desenharLinecast)
		{
			if (!npcLinecast)
			{
				GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
				if (npcs.Count() > 0)
					npcLinecast = npcs[0];
			}

			if (npcLinecast)
				npcLinecast.GetComponent<NPC>().desenharLinecast = true;
		}
		else
		{
			if (npcLinecast)
				npcLinecast.GetComponent<NPC>().desenharLinecast = false;
		}

		if (Input.GetKeyDown(KeyCode.F12) || (controladorCena.jogoPausado && Input.GetKeyDown("joystick button 8")))
			controladorCena.CarregarCena("início");

		if (controladorCena.jogoPausado)
			return;

		AtualizarRelogio();

		AtualizarBotoesAuxilio();
		AtualizarBotoesAuxilioTextos();
		
		bool comandoLiberado = false;
		float delay = delayComando;

		if (cameraBotaoZoomLiberado && controladorCena.state.Buttons.LeftShoulder == ButtonState.Pressed)
		{
			cameraBotaoZoomLiberado = false;
			AlterarCameraZoom();
		}
		else if (!cameraBotaoZoomLiberado && controladorCena.state.Buttons.LeftShoulder == ButtonState.Released)
			cameraBotaoZoomLiberado = true;

		if (jogador.comandosHabilitados && Time.time > tempoUltimoComando)
		{
			if (alterarEstadoBotoesAuxilioLiberado && controladorCena.state.Buttons.RightShoulder == ButtonState.Pressed)
			{
				alterarEstadoBotoesAuxilioLiberado = false;
				AlterarEstadoBotoesAuxilio();
			}
			else if (!alterarEstadoBotoesAuxilioLiberado && controladorCena.state.Buttons.RightShoulder == ButtonState.Released)
				alterarEstadoBotoesAuxilioLiberado = true;

			if (Input.GetButtonDown("InteragirLuz"))
			{
				if (jogador.comandosComunsHabilitados && personagem.luzComodoAtual && Time.time > tempoAlterarLuz)
				{
					StartCoroutine(OscilarLuzComodo());
					comandoLiberado = true;
				}
			}
			else if (Input.GetButtonDown("IniciarInteraçãoObjeto"))
			{
				comandoLiberado = true;

				Objeto objetoMarcado = jogador.objetoMarcado ? jogador.objetoMarcado.GetComponent<Objeto>() : null;
				if (jogador.objetoMarcado && !jogador.objetoInteracao && (objetoMarcado && (objetoMarcado.chamarAtencaoLiberado || (objetoMarcado.assustar && (objetoMarcado.ultimoAssustar == 0 || Time.time > objetoMarcado.ultimoAssustar + objetoMarcado.delayAssustar)))))
				{
					jogador.IniciarInteracao();
				}
				else if (jogador.objetoInteracao)
				{
					jogador.EncerrarInteracao();
				}
				else
					comandoLiberado = false;
			}
			else if (Input.GetButtonDown("Interagir"))
			{
				comandoLiberado = true;
				if (jogador.objetoInteracao)
				{
					Objeto objeto = jogador.objetoInteracao.GetComponent<Objeto>();
					if (!objeto.chamandoAtencao && objeto.chamarAtencaoLiberado)
					{
						//Debug.Log("ChamarAtencao");
						objeto.ChamarAtencao();
					}
					else if (objeto.chamandoAtencao && objeto.encerrarChamarAtencao)
					{
						//Debug.Log("EncerrarChamarAtencao");
						objeto.EncerrarChamarAtencao();
					}
					else if (objeto.assustar && !objeto.assustando && (objeto.ultimoAssustar == 0 || Time.time > objeto.ultimoAssustar + objeto.delayAssustar))
					{
						objeto.Assustar();
					}
					else
						comandoLiberado = false;
				}
				else
					jogador.Assustar();
			}
		}

		bool comandoPressionado = false;

		List<string> teclas = new List<string>(new string[] {
			"InteragirLuz",
			"IniciarInteraçãoObjeto",
			"Interagir"
		});

		foreach (string tecla in teclas)
		{
			if (Input.GetButtonDown(tecla))
			{
				comandoPressionado = true;

				/*
				if (comandoLiberado)
					delay += delayComandoLiberado;
				*/

				break;
			}
		}

		if (comandoLiberado)
			tempoUltimoComando = Time.time + delay;
		else if (comandoPressionado)
		{
			// ComandoDesativado();

			if (Time.time > tempoUltimoComando)
				tempoUltimoComando = Time.time + delayComandoDesativado;
		}
	}

	void AtualizarRelogio()
	{
		TimeSpan tempo = TimeSpan.FromSeconds(Time.time - tempoJogo);
		string tempoText = string.Format("{0:00}:{1:00}", tempo.Minutes, tempo.Seconds);
		relogioText.text = tempoText;
	}

	void AlterarEstadoBotoesAuxilio()
	{
		estadoBotoesAjuda = estadoBotoesAjuda < 2 ? estadoBotoesAjuda + 1 : 0;
		AtualizarExibicaoBotoesAuxilio();
	}

	void AtualizarExibicaoBotoesAuxilio()
	{
		switch (estadoBotoesAjuda)
		{
			case 0:
				botoesAuxilioTextosParent.SetActive(false);
				botaoAuxilioParent.SetActive(false);
				break;
			case 1:
				botaoAuxilioParent.SetActive(true);
				break;
			case 2:
				botoesAuxilioTextosParent.SetActive(true);
				break;
		}
	}

	void AtualizarBotoesAuxilio()
	{
		if (estadoBotoesAjuda == 0)
			return;

		if (jogador.comandosHabilitados && Time.time > tempoUltimoComando)
		{
			if (jogador.comandosComunsHabilitados && personagem.luzComodoAtual && Time.time > tempoAlterarLuz)
				botoesAuxilio["X"].AlterarImagem(true);
			else
				botoesAuxilio["X"].AlterarImagem(false);
			
			Objeto objetoMarcado = jogador.objetoMarcado ? jogador.objetoMarcado.GetComponent<Objeto>() : null;
			if (jogador.objetoInteracao || (objetoMarcado && (objetoMarcado.chamarAtencaoLiberado || (objetoMarcado.assustar && (objetoMarcado.ultimoAssustar == 0 || Time.time > objetoMarcado.ultimoAssustar + objetoMarcado.delayAssustar)))))
				botoesAuxilio["A"].AlterarImagem(true);
			else
				botoesAuxilio["A"].AlterarImagem(false);
			
			Objeto objeto = jogador.objetoInteracao ? jogador.objetoInteracao.GetComponent<Objeto>() : null;
			if (!jogador.objetoInteracao || (objeto &&
				((!objeto.chamandoAtencao && objeto.chamarAtencaoLiberado) ||
				(objeto.chamandoAtencao && objeto.encerrarChamarAtencao) ||
				(objeto.assustar && !objeto.assustando && (objeto.ultimoAssustar == 0 || Time.time > objeto.ultimoAssustar + objeto.delayAssustar)))))
				botoesAuxilio["B"].AlterarImagem(true);
			else
				botoesAuxilio["B"].AlterarImagem(false);
		}
		else
			foreach (KeyValuePair<string, BotaoAuxilio> botaoAuxilio in botoesAuxilio)
				botaoAuxilio.Value.AlterarImagem(false);
	}

	void AtualizarBotoesAuxilioTextos()
	{
		if (estadoBotoesAjuda == 0)
			return;

		if (jogador.comandosHabilitados && Time.time > tempoUltimoComando)
		{
			if (jogador.comandosComunsHabilitados && personagem.luzComodoAtual && Time.time > tempoAlterarLuz)
				botoesAuxilioTextos["X"].text = "Luz";
			else
				botoesAuxilioTextos["X"].text = "";

			Objeto objetoMarcado = jogador.objetoMarcado ? jogador.objetoMarcado.GetComponent<Objeto>() : null;
			if (jogador.objetoInteracao)
				botoesAuxilioTextos["A"].text = "Encerrar Interação";
			else if (objetoMarcado && (objetoMarcado.chamarAtencaoLiberado || (objetoMarcado.assustar && (objetoMarcado.ultimoAssustar == 0 || Time.time > objetoMarcado.ultimoAssustar + objetoMarcado.delayAssustar))))
				botoesAuxilioTextos["A"].text = "Iniciar Interação";
			else
				botoesAuxilioTextos["A"].text = "";

			Objeto objeto = jogador.objetoInteracao ? jogador.objetoInteracao.GetComponent<Objeto>() : null;
			if (!objeto || (objeto && !objeto.chamarAtencaoLiberado && objeto.assustar && !objeto.assustando && (objeto.ultimoAssustar == 0 || Time.time > objeto.ultimoAssustar + objeto.delayAssustar)))
				botoesAuxilioTextos["B"].text = "Assustar";
			else if (objeto &&
				((!objeto.chamandoAtencao && objeto.chamarAtencaoLiberado) ||
				(objeto.chamandoAtencao && objeto.encerrarChamarAtencao)))
			{
				botoesAuxilioTextos["B"].text = "Chamar Atenção";
			}
			else
				botoesAuxilioTextos["B"].text = "";
		}
		else
			foreach (KeyValuePair<string, Text> botaoAuxilioTexto in botoesAuxilioTextos)
				botaoAuxilioTexto.Value.text = ""; ;
	}

		void ComandoDesativado()
	{
		audioSource.clip = comandoDesativado;
		audioSource.Play();
	}
	
	IEnumerator IniciarJogo()
	{
		yield return new WaitForSeconds(delayInicioJogo);
		StartCoroutine(InstanciarNPCs());
	}

	IEnumerator InstanciarNPCs()
	{
		Transform npcs = GameObject.Find("NPCs").transform;

		int npcId = 1;

		while (!vitoria)
		{
			if (quantidadeNpcsRestantes > 0)
			{
				GameObject npcInstanciado = (GameObject)Instantiate(npc, waypointInicial.transform.position, npc.transform.rotation);

				npcsEmJogo++;

				NPC npcScript = npcInstanciado.transform.FindChild("NPC").GetComponent<NPC>();
				npcScript.delayInicial = UnityEngine.Random.Range(delayInicialNpcs.min, delayInicialNpcs.max);
				npcScript.delayMovimento = UnityEngine.Random.Range(delayMovimentoNpcs.min, delayMovimentoNpcs.max);
				npcScript.delayInteracaoObjeto = UnityEngine.Random.Range(delayInteracaoObjetoNpcs.min, delayInteracaoObjetoNpcs.max);

				npcInstanciado.transform.parent = npcs;
				npcInstanciado.name = "NPC (" + npcId + ")";
				npcId++;
				quantidadeNpcsRestantes--;
			}

			if (quantidadeNpcsSairam > 0)
			{
				quantidadeNpcsRestantes += quantidadeNpcsSairam;
				quantidadeNpcsSairam = 0;
			}

			yield return new WaitForSeconds(UnityEngine.Random.Range(delayEntradaNpcs.min, delayEntradaNpcs.max));
		}
	}

	public void AlterarLuzComodo(MeshRenderer luzComodo = null)
	{
		if (luzComodo == null)
			luzComodo = personagem.luzComodoAtual;

		float intensidadeLuz = luz.apagada;
		bool luzApagada = false;
		if (luzComodo.material.color.a == luz.apagada)
		{
			intensidadeLuz = luz.acesa;
			luzApagada = true;
		}
		
		luzComodo.GetComponent<AudioSource>().Play();

		AlterarLuzApagada(luzApagada, luzComodo);
		luzComodo.material.color = new Vector4(0, 0, 0, intensidadeLuz);
	}

	IEnumerator OscilarLuzComodo()
	{
		float oscilarInicio = Time.time;
		luzOscilacao = personagem.luzComodoAtual;
		Vector4 corInicial = luzOscilacao.material.color;

		jogador.comandosHabilitados = false;
		
		personagem.luzComodoAtual.GetComponent<AudioSource>().Play();

		AlterarLuzApagada(true);
		while (true)
		{
			luzOscilacao.material.color = new Vector4(0, 0, 0, UnityEngine.Random.Range(luz.oscilandoMin, luz.oscilandoMax));

			if (Time.time >= oscilarInicio + oscilarDuracao)
			{
				luzOscilando = false;
				luzOscilacao.material.color = corInicial;
				tempoAlterarLuz = Time.time + delayAlterarLuz;
				jogador.comandosHabilitados = true;
				break;
			}

			yield return new WaitForSeconds(0.1f);
		}
	}

	void AlterarLuzApagada(bool luzApagada, MeshRenderer luzComodoAtual = null)
	{
		if (!luzComodoAtual)
			luzComodoAtual = personagem.luzComodoAtual;

		Comodo comodo = luzComodoAtual.transform.parent.GetComponent<Comodo>();

		comodo.luzApagada = luzApagada;

		comodo.AlterarLuz(luzApagada);
	}

	public static GameObject PegarObjeto()
	{
		return GameObject.Find("ControladorJogo");
	}

	public static Jogo Pegar()
	{
		return PegarObjeto().GetComponent<Jogo>();
	}

	public Vector2 PegarVariacaoWaypoint()
	{
		return variacaoWaypoint;
	}

	public void ColetarAlma(GameObject fonte)
	{
		GameObject instanciaFX = Instantiate(coletarAlmaFX, fonte.transform.position, fonte.transform.rotation) as GameObject;
		almas++;
		AtualizarAlmas();
		
		if (almas == objetivo)
			StartCoroutine(Vitoria());
	}

	IEnumerator Vitoria()
	{
		jogador.animator.SetBool("assustar", true);

		audioSourceEfeitos.clip = audioVitoria;
		audioSourceEfeitos.loop = false;
		audioSourceEfeitos.Play();

		jogador.comandosHabilitados = false;
		jogador.comandosComunsHabilitados = false;
		
		StartCoroutine(FlutuarPersonagem());

		vitoria = true;

		GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
		foreach (GameObject npc in npcs)
		{
			NPC npcScript = npc.GetComponent<NPC>();
			if (!npcScript.morto)
			{
				npcScript.reacaoLiberada = false;
				npcScript.saindo = true;
				npcScript.PararMovimento();
				npcScript.caminho = Waypoint.DefinirCaminho(npcScript.waypointAtual, waypointInicial, true);
			}
		}

		yield return new WaitForSeconds(5f);

		telaVitoria.SetActive(true);
		jogador.comandosHabilitados = false;
		jogador.comandosComunsHabilitados = false;

		float tempoTotal = Mathf.Max(1, Time.time - tempoJogo);
		float pontos = Mathf.Floor(Mathf.Max(almas * 18, almas * 30 + (60 * 10 - tempoTotal)));

		StartCoroutine(AtualizarPontuacao(pontos));

		camera.orthographicSize = cameraSizeNormal;
		camera.transform.position = cameraPosicaoFinal;
	}

	IEnumerator FlutuarPersonagem()
	{
		Vector3 posicao1 = new Vector3(
			jogador.fantasma.transform.position.x,
			jogador.fantasma.transform.position.y + 0.2f,
			jogador.fantasma.transform.position.z
		);

		Vector3 posicao2 = new Vector3(
			jogador.fantasma.transform.position.x,
			jogador.fantasma.transform.position.y - 0.2f,
			jogador.fantasma.transform.position.z
		);

		while (true)
		{
			Vector3 posicao;
			if (jogador.fantasma.transform.position == posicao2)
				posicao = posicao1;
			else if (jogador.fantasma.transform.position == posicao1)
				posicao = posicao2;
			else
				posicao = posicao1;

			iTween.MoveTo(jogador.fantasma, iTween.Hash("position", posicao, "easeType", iTween.EaseType.linear, "time", 0.29f));

			yield return new WaitForSeconds(0.32f);
		}
	}

	IEnumerator AtualizarPontuacao(float totalPontos)
	{
		float pontuacaoAnterior = 0;
		float intervaloMax = Mathf.Max(2, totalPontos / 15);
		float intervaloMin = Mathf.Max(1, intervaloMax - 10);
		float delay = 0.01f;
		while (true)
		{
			var pontos = Mathf.Min(pontuacaoAnterior + Mathf.Floor(UnityEngine.Random.Range(intervaloMin, intervaloMax)), totalPontos);
			pontuacaoAnterior = pontos;

			GameObject.Find("Pontuação").GetComponent<Text>().text = pontos.ToString();
			
			if (pontos == totalPontos)
				break;

			delay = Mathf.Min(delay + 0.02f, 0.3f);

			yield return new WaitForSeconds(delay);
		}

		anyKeyRestart = true;
	}

	void AtualizarAlmas()
	{
		foreach (Animator almaAnimator in almasHUD)
		{
			if (!almaAnimator.GetBool("coletada"))
			{
				almaAnimator.SetBool("coletada", true);
				break;
			}
			else
			{
				almaAnimator.Rebind();
				almaAnimator.SetBool("coletada", true);
			}
		}
	}

	public void AtualizarVidas()
	{
		foreach (Animator vidaAnimator in vidasHUD)
		{
			if (!vidaAnimator.GetBool("desativada"))
			{
				vidaAnimator.SetBool("desativada", true);
				break;
			}
		}
	}

	void AlterarCameraZoom()
	{
		if (cameraZoomLiberado)
		{
			if (jogador.comandosComunsHabilitados)
			{
				cameraLiberarComandosJogador = true;
				jogador.comandosHabilitados = false;
				jogador.comandosComunsHabilitados = false;
			}

			cameraZoomLiberado = false;
			float orthographicSize = cameraSizeNormal;
			StartCoroutine(CameraZoom());
			cameraTravada = !cameraTravada;
		}
	}

	IEnumerator CameraZoom()
	{
		float cameraSize = camera.orthographicSize;
		float cameraSizeFinal;

		Vector3 cameraPosicao = camera.transform.position;
		Vector3 cameraPosicaoZoom;

		if (cameraTravada)
		{
			cameraSizeFinal = cameraSizeNormal;
			cameraPosicaoZoom = cameraPosicaoNormal;
		}
		else
		{
			cameraSizeFinal = cameraSizeZoomOut;
			cameraPosicaoZoom = cameraPosicaoZoomOut;
		}

		Vector3 cameraPosicaoFinal = new Vector3(cameraPosicaoZoom.x, cameraPosicaoZoom.y, camera.transform.position.z);

		float intervaloCameraSize = (cameraSizeFinal - cameraSize) / fpsZoom;
		Vector3 intervaloCameraPosicao = (cameraPosicaoFinal - cameraPosicao) / fpsZoom;

		Vector2 cameraSizeFinalLimites = new Vector2(
			(cameraSizeFinal > cameraSize) ? cameraSize : cameraSizeFinal,
			(cameraSizeFinal > cameraSize) ? cameraSizeFinal : cameraSize
		);

		Vector2 cameraPosicaoFinalLimitesX = new Vector2(
			(cameraPosicaoFinal.x > cameraPosicao.x) ? cameraPosicao.x : cameraPosicaoFinal.x,
			(cameraPosicaoFinal.x > cameraPosicao.x) ? cameraPosicaoFinal.x : cameraPosicao.x
		);

		Vector2 cameraPosicaoFinalLimitesY = new Vector2(
			(cameraPosicaoFinal.y > cameraPosicao.y) ? cameraPosicao.y : cameraPosicaoFinal.y,
			(cameraPosicaoFinal.y > cameraPosicao.y) ? cameraPosicaoFinal.y : cameraPosicao.y
		);

		while (true)
		{
			camera.orthographicSize = Mathf.Clamp(camera.orthographicSize + intervaloCameraSize, cameraSizeFinalLimites.x, cameraSizeFinalLimites.y);

			camera.transform.position = new Vector3(
				Mathf.Clamp(camera.transform.position.x + intervaloCameraPosicao.x, cameraPosicaoFinalLimitesX.x, cameraPosicaoFinalLimitesX.y),
				Mathf.Clamp(camera.transform.position.y + intervaloCameraPosicao.y, cameraPosicaoFinalLimitesY.x, cameraPosicaoFinalLimitesY.y),
				cameraPosicaoFinal.z
			);

			if (camera.orthographicSize == cameraSizeFinal && camera.transform.position == cameraPosicaoFinal)
				break;

			yield return new WaitForSeconds(duracaoZoom / fpsZoom);
		}

		cameraZoomLiberado = true;

		if (!cameraTravada && cameraLiberarComandosJogador)
		{
			cameraLiberarComandosJogador = false;
			jogador.comandosHabilitados = true;
			jogador.comandosComunsHabilitados = true;
		}
	}

	public void AtualizarPosicaoCameraJogador()
	{
		if (!cameraTravada)
		{
			camera.transform.position = new Vector3(
				jogador.transform.position.x,
				jogador.transform.position.y,
				camera.transform.position.z
			);

			cameraPosicaoNormal = camera.transform.position;
		}
	}
}
