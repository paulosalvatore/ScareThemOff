using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NPC : MonoBehaviour
{
	[Header("Delays")]
	[DisplayWithoutEdit()] public float delayInicial;
	[DisplayWithoutEdit()] public float delayMovimento;
	[DisplayWithoutEdit()] public float delayInteracaoObjeto;

	[Header("Movimento")]
	[DisplayWithoutEdit()] public bool emMovimento;
	private float duracaoMovimento;
	private bool movimentar;
	private bool movimentoLiberado = true;
	private bool liberacaoIniciada;
	private bool viradoDireita;

	[Header("Barra de Tensão")]
	public float tensaoSusto;
	public float maxTensao;
	public float delayMorrer;
	internal float tensaoAtual = 0;
	private GameObject barraTensao;
	private GameObject conteudoBarraTensao;
	private float maxScale;
	private List<string> objetosTensao = new List<string>();
	internal GameObject objetoInteracao;
	private bool modificadorTensaoLiberado = true;

	[Header("Visão dos NPCs")]
	[DisplayWithoutEdit()] public List<string> visaoNPCsExibicao = new List<string>();
	internal Dictionary<GameObject, bool> visaoNPCs = new Dictionary<GameObject, bool>();
	public bool desenharLinecast;
	[DisplayWithoutEdit()] public bool visaoJogador;

	internal GameObject waypointAtual;
	private GameObject waypointAnterior;
	internal List<GameObject> caminho = new List<GameObject>();

	private Jogo jogo;
	private GameObject jogador;
	private Jogador jogadorScript;
	private Personagem personagem;
	private Personagem npc;
	private Exorcista exorcista;
	private Animator animator;
	private GameObject balaoReacao;
	private GameObject emoticon;
	private AudioSource audioSource;
	private SpriteRenderer spriteRenderer;
	private List<Sprite> cabelos = new List<Sprite>();
	private List<Sprite> camisas = new List<Sprite>();
	private List<Sprite> bermudas = new List<Sprite>();
	private SpriteRenderer cabelo;
	private SpriteRenderer camisa;
	private SpriteRenderer bermuda;

	[Header("Direção")]
	private int direcao = 1;
	[DisplayWithoutEdit()] public string exibirDirecao;
	[DisplayWithoutEdit()] public string exibirOrientacao;
	private bool atualizarDirecaoAposMovimento;

	internal bool morto;
	internal bool assustado;
	internal bool saindo;
	private bool coletado;

	[Header("Chances")]
	public float chanceObjetoAtrair;
	public float chanceSairJogo;
	public float chanceSairComodo;
	public float chanceWaypointAleatorioComodo;

	public float chanceReacao;
	internal bool reacaoLiberada;
	internal string tipoReacao;

	internal List<GameObject> objetosVisitados = new List<GameObject>();
	private bool interagindoObjeto;
	
	public bool chamarExorcista;

	internal GameObject chamadoComodo = null;

	public bool debug;
	internal List<string> debugInfo = new List<string>();

	void Start ()
	{
		jogo = Jogo.Pegar();

		int cabeloId = Random.Range(0, jogo.cabelos.frente.Count);
		int camisaId = Random.Range(0, jogo.camisas.frente.Count);
		int bermudaId = Random.Range(0, jogo.bermudas.frente.Count);

		cabelos.Add(jogo.cabelos.frente[cabeloId]);
		cabelos.Add(jogo.cabelos.costas[cabeloId]);

		camisas.Add(jogo.camisas.frente[camisaId]);
		camisas.Add(jogo.camisas.costas[camisaId]);

		bermudas.Add(jogo.bermudas.frente[bermudaId]);
		bermudas.Add(jogo.bermudas.costas[bermudaId]);

		cabelo = transform.FindChild("Cabelo").GetComponent<SpriteRenderer>();
		camisa = transform.FindChild("Camisa").GetComponent<SpriteRenderer>();
		bermuda = transform.FindChild("Bermuda").GetComponent<SpriteRenderer>();

		cabelo.sprite = cabelos[0];
		camisa.sprite = camisas[0];
		bermuda.sprite = bermudas[0];

		jogador = Jogador.PegarObjeto();
		jogadorScript = Jogador.PegarControlador();
		personagem = Personagem.PegarControlador("Jogador");
		npc = Personagem.PegarControlador(transform.parent.name, true);
		exorcista = jogo.exorcista;

		balaoReacao = transform.parent.FindChild("BalaoReacao").gameObject;
		emoticon = transform.parent.FindChild("Emoticon").gameObject;

		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		audioSource = GetComponent<AudioSource>();

		waypointAtual = jogo.waypointInicial;
		npc.DefinirComodoAtual(waypointAtual.GetComponent<Waypoint>().comodo);

		transform.parent.position = VariarPosicao();

		StartCoroutine(MovimentoInicial());
		
		barraTensao = transform.parent.FindChild("BarraTensao").gameObject;
		conteudoBarraTensao = barraTensao.transform.FindChild("Conteudo").gameObject;
		maxScale = conteudoBarraTensao.transform.localScale.x;
		AtualizarBarraTensao();

		AtualizarDirecao();

		chanceSairComodo += chanceSairJogo;
		chanceObjetoAtrair += chanceSairComodo;
		chanceWaypointAleatorioComodo += chanceObjetoAtrair;
	}

	void OnDrawGizmosSelected()
	{
		/*if (desenharLinecast)
		{
			Gizmos.color = Color.green;
			GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
			foreach (GameObject npc in npcs)
				Gizmos.DrawLine(transform.position, (npc.transform.position));
		}*/
	}

	void Update ()
	{
		if (debug)
		{
			foreach(string texto in debugInfo)
				Debug.Log(texto);

			debug = false;
		}
		
		AtualizarVisaoNPCs();
		Movimentar();
		Reacao();

		ChecarLiberacaoMovimento();
	}

	void OnTriggerEnter2D(Collider2D coll)
	{
		if (coll.CompareTag("Player") && morto && !coletado)
		{
			coletado = true;
			StartCoroutine(NpcSaiu(false));
			jogo.ColetarAlma(gameObject);
		}
	}

	void Reacao()
	{
		if (reacaoLiberada)
		{
			bool gerarReacao = false;

			float chance = Random.Range(1, 100);
			if (tipoReacao == "morto" || tipoReacao == "tensao" || (tipoReacao == "objeto" && chance < chanceReacao))
				gerarReacao = true;
			
			if (gerarReacao)
			{
				AtivarBalaoReacao();
				Emoticon emoticonScript = emoticon.GetComponent<Emoticon>();
				
				if (tipoReacao == "objeto" && interagindoObjeto && !emMovimento && caminho.Count() == 0)
					emoticonScript.ReacaoObjeto();
				else if (tipoReacao == "tensao")
					emoticonScript.ReacaoTensao();
				//else if (tipoReacao == "morto")
				//	emoticonScript.ReacaoMorto();
				else
				{
					DesativarBalaoReacao();
					return;
				}
				reacaoLiberada = false;
			}
			else
				DesativarBalaoReacao();
		}
	}

	void AtivarBalaoReacao()
	{
		if (!assustado)
		{
			balaoReacao.SetActive(true);
			emoticon.SetActive(true);
		}
	}

	public void DesativarBalaoReacao()
	{
		balaoReacao.SetActive(false);
		emoticon.SetActive(false);
	}

	void Movimentar()
	{
		if ((tipoReacao == "objeto" && emMovimento) ||
			(tipoReacao == "movimento" && !emMovimento) ||
			(tipoReacao == "tensao") && tensaoAtual == 0)
		{
			DesativarBalaoReacao();
		}

		if (caminho.Count() > 0 && !emMovimento)
		{
			if (movimentoLiberado)
			{
				waypointAnterior = waypointAtual;
				waypointAtual = caminho[0];
				AdicionarDebugInfo("Processando caminho: " + waypointAnterior.name + " para " + waypointAtual.name);
				caminho.RemoveAt(0);

				MoverParaWaypoint(caminho.Count() == 0 ? true : false, true, caminho.Count() == 0 ? waypointAtual.transform.parent.gameObject : null);
				
				if (saindo && caminho.Count == 0)
				{
					AdicionarDebugInfo("NpcSaiu chamado em (saindo && caminho.Count == 0)");
					StartCoroutine(NpcSaiu());
					return;
				}
				else if (chamarExorcista && caminho.Count == 0)
				{
					bool checarChamarExorcista = true;
					GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
					foreach (GameObject npc in npcs)
					{
						if (npc != gameObject)
						{
							NPC npcScript = npc.GetComponent<NPC>();
							if (npcScript.chamarExorcista)
							{
								checarChamarExorcista = false;
								break;
							}
						}
					}

					if (checarChamarExorcista)
						StartCoroutine(PrepararExorcista());
					else
						chamarExorcista = false;

					AdicionarDebugInfo("NpcSaiu chamado em (saindo && caminho.Count == 0)");
					StartCoroutine(NpcSaiu());
				}

			}
			else
				movimentoLiberado = true;

			return;
		}

		if (movimentar)
		{
			if (movimentoLiberado)
			{
				float chance = Random.Range(1, 100);

				if (chamadoComodo)
					chance = chanceSairComodo;

				if (chance <= chanceSairJogo && waypointAtual != jogo.waypointInicial)
				{
					caminho = Waypoint.DefinirCaminho(waypointAtual, jogo.waypointInicial, true);
					saindo = true;
				}
				else if (chance <= chanceSairComodo)
				{
					GameObject comodoSelecionado = null;
					if (chamadoComodo)
					{
						comodoSelecionado = chamadoComodo;
						chamadoComodo = null;
					}
					else
					{
						List<GameObject> comodos = Comodo.PegarComodos();

						Dictionary<GameObject, float> comodosDistancia = new Dictionary<GameObject, float>();

						foreach (GameObject comodo in comodos)
							if (comodo != npc.comodoAtual && !comodo.GetComponent<Comodo>().luzApagada)
								comodosDistancia.Add(comodo, Vector3.Distance(transform.position, comodo.transform.position));

						if (comodosDistancia.Count > 0)
						{
							var comodosDistanciaOrdenados = from pair in comodosDistancia
															orderby pair.Value ascending
															select pair;

							var comodosProximos = comodosDistanciaOrdenados.ToList();

							int maxComodos = Mathf.Min(2, comodosProximos.Count());

							List<GameObject> comodosVisitar = new List<GameObject>();

							for (int i = 0; i < maxComodos; i++)
							{
								comodosVisitar.Add(comodosProximos.First().Key);
								comodosProximos.RemoveAt(0);
							}

							comodoSelecionado = comodosVisitar.PickRandom();
						}
					}

					if (comodoSelecionado)
					{
						if (!CalcularSairComodo(comodoSelecionado))
							return;

						List<GameObject> waypoints = Waypoint.PegarWaypoints(comodoSelecionado, true);

						Dictionary<GameObject, float> waypointsDistancia = new Dictionary<GameObject, float>();

						foreach (GameObject waypoint in waypoints)
							waypointsDistancia.Add(waypoint, Vector3.Distance(transform.position, waypoint.transform.position));

						var waypointsDistanciaOrdenados = from pair in waypointsDistancia
														  orderby pair.Value ascending
														  select pair;

						var waypointsProximos = waypointsDistanciaOrdenados.ToList();

						int maxWaypoints = Mathf.Min(3, waypointsProximos.Count());

						List<GameObject> waypointsSelecionados = new List<GameObject>();

						for (int i = 0; i < maxWaypoints; i++)
						{
							waypointsSelecionados.Add(waypointsProximos.First().Key);
							waypointsProximos.RemoveAt(0);
						}

						if (waypointsSelecionados.Count() > 0)
						{
							GameObject waypointSelecionado = waypointsSelecionados.PickRandom();

							caminho = Waypoint.DefinirCaminho(waypointAtual, waypointSelecionado, true);
						}
					}
				}
				else if (chance <= chanceObjetoAtrair)
				{
					Comodo comodo = waypointAtual.GetComponent<Waypoint>().comodo.GetComponent<Comodo>();
					List<GameObject> objetosComodo = comodo.objetosComodo;

					Dictionary<GameObject, float> objetosDistancia = new Dictionary<GameObject, float>();

					foreach (GameObject objeto in objetosComodo)
						if (!objetosVisitados.Contains(objeto))
							objetosDistancia.Add(objeto, Vector3.Distance(objeto.transform.position, waypointAtual.transform.position));
					
					if (objetosDistancia.Count() > 0)
					{
						var objetosDistanciaOrdenados = from pair in objetosDistancia
														orderby pair.Value ascending
														select pair;

						var objetosProximos = objetosDistanciaOrdenados.ToList();

						int maxObjetos = Mathf.Min(3, objetosProximos.Count());

						List<GameObject> objetosVisitar = new List<GameObject>();

						for (int i = 0; i < maxObjetos; i++)
						{
							objetosVisitar.Add(objetosProximos.First().Key);
							objetosProximos.RemoveAt(0);
						}

						GameObject visitarObjeto = objetosVisitar.PickRandom(1).First();

						objetosVisitados.Add(visitarObjeto);

						GameObject waypointChildren = null;
						foreach (Transform child in visitarObjeto.transform)
						{
							if (child.CompareTag("Waypoint"))
							{
								waypointChildren = child.gameObject;
								break;
							}
						}

						if (waypointChildren)
						{
							caminho = Waypoint.DefinirCaminho(waypointAtual, waypointChildren, true);

							tipoReacao = "objeto";
							interagindoObjeto = true;
							reacaoLiberada = true;
						}
					}
					else
					{
						//objetosVisitados = new List<GameObject>();

						// Colocar uma chance de zerar os objetos visitados no cômodo
						// Criar uma lista de objetos visitados dependendo do cômodo em questão
					}
				}
				else if (chance <= chanceWaypointAleatorioComodo)
				{
					bool waypointDisponivel = false;
					List<GameObject> waypointsDisponiveis = Waypoint.PegarWaypoints(npc.comodoAtual, true);

					while (!waypointDisponivel && waypointsDisponiveis.Count > 0)
					{
						GameObject waypoint = waypointsDisponiveis.PickRandom();

						if (!waypoint.GetComponent<Waypoint>().travarMovimento)
						{
							waypointDisponivel = true;
							caminho = Waypoint.DefinirCaminho(waypointAtual, waypoint, true);
						}
						else
							waypointsDisponiveis.Remove(waypoint);
					}
				}
				else
				{
					AdicionarDebugInfo("Ir para waypoint adjacente.");
					waypointAnterior = waypointAtual;

					List<GameObject> waypointsDisponiveis = waypointAtual.GetComponent<Waypoint>().waypointsDisponiveis.ToList();

					if (waypointsDisponiveis.Count == 0)
						waypointsDisponiveis = Waypoint.PegarWaypoints(personagem.comodoAtual);

					bool comodoDisponivel = false;

					while (!comodoDisponivel && waypointsDisponiveis.Count > 0)
					{
						GameObject waypointSelecionado = waypointsDisponiveis[Random.Range(0, waypointsDisponiveis.Count)];
						Waypoint waypointScript = waypointSelecionado.GetComponent<Waypoint>();
						GameObject comodoWaypoint = waypointScript.comodo;

						bool luzApagada = false;
						if (comodoWaypoint)
							luzApagada = comodoWaypoint.GetComponent<Comodo>().luzApagada;

						bool checarNpcComodo = true;
						if (comodoWaypoint != npc.comodoAtual)
						{
							AdicionarDebugInfo("Calcular para Sair do Cômodo");
							checarNpcComodo = CalcularSairComodo(comodoWaypoint);
						}

						if (checarNpcComodo && !waypointScript.travarMovimento && (waypointScript.objeto || !luzApagada))
						{
							comodoDisponivel = true;
							AdicionarDebugInfo("comodoDisponivel");
							waypointAtual = waypointSelecionado;
							break;
						}
						else
							waypointsDisponiveis.Remove(waypointSelecionado);

						AdicionarDebugInfo("waypointsDisponiveis.Count: " + waypointsDisponiveis.Count);
					}

					AdicionarDebugInfo("waypointAtual.name: " + waypointAtual.name + " - waypointAnterior.name: " + waypointAnterior.name);

					MoverParaWaypoint();
				}
			}
			else if (!emMovimento)
			{
				if (waypointAtual.GetComponent<Waypoint>().movimentoAutomatico)
				{
					List<GameObject> waypointsDisponiveis = waypointAtual.GetComponent<Waypoint>().waypointsDisponiveis;
					foreach (GameObject waypoint in waypointsDisponiveis)
					{
						if (waypoint.name != waypointAnterior.name)
						{
							bool liberado = false;
							GameObject checarWaypoint = waypoint;
							while (!liberado)
							{
								Waypoint waypointScript = checarWaypoint.GetComponent<Waypoint>();
								if (waypointScript.movimentoAutomatico)
								{
									foreach (GameObject wp in waypointScript.waypointsDisponiveis)
										if (wp.name != checarWaypoint.name)
											checarWaypoint = wp;
								}
								else
								{
									if (CalcularSairComodo(waypointScript.comodo))
										liberado = true;

									break;
								}
							}

							if (liberado)
							{
								waypointAnterior = waypointAtual;
								waypointAtual = waypoint;
								MoverParaWaypoint();
								break;
							}
						}
					}
				}
				else if (!liberacaoIniciada)
					StartCoroutine(LiberacaoMovimento());
				else if (interagindoObjeto)
				{
					StartCoroutine(AtualizarAnimacao("foto", true, 2f));
					interagindoObjeto = false;
				}
			}
		}
		else if (objetoInteracao && atualizarDirecaoAposMovimento && !emMovimento)
		{
			AtualizarDirecao(objetoInteracao.transform.position);
			atualizarDirecaoAposMovimento = false;
		}
	}

	bool CalcularSairComodo(GameObject comodo)
	{
		Comodo comodoScript = comodo.GetComponent<Comodo>();
		float quantidadeNpcsComodo = comodoScript.npcsComodo.Count;

		if (comodoScript.limiteNpcs && quantidadeNpcsComodo > 0)
		{
			float chanceComodo = Random.Range(1, 100);
			float chanceNpcAdicionalComodo = Mathf.Max (1, jogo.chanceNpcAdicionalComodo - (quantidadeNpcsComodo * 2));

			if (chanceComodo > chanceNpcAdicionalComodo)
				return false;
		}

		return true;
	}

	public void ChecarTensao()
	{
		if (modificadorTensaoLiberado)
		{
			if (objetoInteracao)
				ReduzirTensao(objetoInteracao.name);
			else
				ReduzirTensao("Luz - " + personagem.comodoAtual.name);

			objetoInteracao = null;
		}
	}

	void ChecarLiberacaoMovimento()
	{
		if (!assustado && !morto && !movimentar && !objetoInteracao && personagem.comodoAtual && !personagem.comodoAtual.GetComponent<Comodo>().luzApagada)
			movimentar = true;
	}

	void AtualizarVisaoNPCs()
	{
		GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
		visaoNPCsExibicao = new List<string>();
		visaoNPCs = new Dictionary<GameObject, bool>();
		foreach (GameObject npc in npcs)
		{
			if (npc != gameObject)
			{
				var cor = Color.red;

				bool visaoPersonagem = !Physics2D.Linecast(transform.position, npc.transform.position, 1 << LayerMask.NameToLayer("Parede"));

				float distanciaPersonagem = Vector3.Distance(transform.position, npc.transform.position);
				if (visaoPersonagem && distanciaPersonagem > jogo.distanciaMaxima)
				{
					visaoPersonagem = false;
					cor = Color.yellow;
				}

				visaoNPCs.Add(npc, visaoPersonagem);
				visaoNPCsExibicao.Add(npc.transform.parent.name + ": " + visaoPersonagem.ToString());

				if (desenharLinecast)
					Debug.DrawLine(transform.position, npc.transform.position, visaoPersonagem ? Color.green : cor);
			}
		}

		visaoJogador = !Physics2D.Linecast(transform.position, jogador.transform.position, 1 << LayerMask.NameToLayer("Parede"));

		float distanciaJogador = Vector3.Distance(transform.position, jogador.transform.position);
		if (distanciaJogador > jogo.distanciaMaxima)
			visaoJogador = false;

		if (desenharLinecast)
			Debug.DrawLine(transform.position, jogador.transform.position, visaoJogador ? Color.cyan : Color.magenta);
	}

	Vector3 VariarPosicao()
	{
		Vector3 posicaoInicial = waypointAtual.transform.position;

		Vector2 variacaoWaypoint;

		Waypoint waypoint = waypointAtual.GetComponent<Waypoint>();

		if (waypoint.variarPosicao)
		{
			if(waypoint.variacao != default(Vector2))
				variacaoWaypoint = waypointAtual.GetComponent<Waypoint>().variacao;
			else
				variacaoWaypoint = jogo.PegarVariacaoWaypoint();

			posicaoInicial.x += Random.Range(-variacaoWaypoint.x, variacaoWaypoint.x);
			posicaoInicial.y += Random.Range(-variacaoWaypoint.y, variacaoWaypoint.y);
		}

		return posicaoInicial;
	}

	void MoverParaWaypoint(bool variarPosicao = true, bool ignorarAlteracoes = false, GameObject objetoDirecao = null)
	{
		AdicionarDebugInfo("MoverParaWaypoint: " + waypointAtual.name);

		Waypoint waypointScript = waypointAtual.GetComponent<Waypoint>();
		GameObject comodoWaypoint = waypointScript.comodo;
		Comodo comodo = comodoWaypoint.GetComponent<Comodo>();
		npc.DefinirComodoAtual(comodoWaypoint);

		/*if (modificadorTensaoLiberado && waypointAnterior != waypointAtual && personagem.comodoAtual)
		{
			if (comodo.luzApagada)
				AumentarTensao("Luz - " + personagem.comodoAtual.name);
			else
				ReduzirTensao("Luz - " + personagem.comodoAtual.name);
		}*/

		Vector3 posicao = waypointAtual.transform.position;
		if (variarPosicao)
			posicao = VariarPosicao();
		
		AtualizarDirecao(posicao);
		AtualizarAnimacao("movimento", true);

		duracaoMovimento = Vector3.Distance(transform.parent.position, posicao) / (chamarExorcista ? 1.5f : 1f);

		iTween.MoveTo(transform.parent.gameObject, iTween.Hash("position", posicao, "easeType", iTween.EaseType.linear, "time", duracaoMovimento));
		
		StartCoroutine(AlterarEmMovimento());

		if (objetoDirecao)
			StartCoroutine(AtualizarDirecaoObjeto(objetoDirecao));
	}

	IEnumerator AlterarEmMovimento()
	{
		movimentoLiberado = false;
		emMovimento = true;

		yield return new WaitForSeconds(duracaoMovimento);

		AtualizarAnimacao("movimento", false);
		emMovimento = false;
	}

	IEnumerator MovimentoInicial()
	{
		yield return new WaitForSeconds(delayInicial);
		movimentar = true;
	}

	IEnumerator LiberacaoMovimento()
	{
		liberacaoIniciada = true;

		yield return new WaitForSeconds(interagindoObjeto ? delayInteracaoObjeto : delayMovimento);
		movimentoLiberado = true;
		liberacaoIniciada = false;
	}

	IEnumerator AtualizarDirecaoObjeto(GameObject objeto)
	{
		yield return new WaitForSeconds(duracaoMovimento);
		AtualizarDirecao(objeto.transform.position);
	}

	void AtualizarBarraTensao()
	{
		/*if (maxTensao > 0)
		{
			float xScale = tensaoAtual / maxTensao;
			float xPosition = (maxScale - xScale) / -2;

			Vector3 localScale = conteudoBarraTensao.transform.localScale;
			conteudoBarraTensao.transform.localScale = new Vector3(
				xScale,
				localScale.y,
				localScale.z
			);

			Vector3 position = conteudoBarraTensao.transform.localPosition;
			conteudoBarraTensao.transform.localPosition = new Vector3(
				xPosition,
				position.y,
				position.z
			);
		}*/
	}

	public void AumentarTensao(string tipoTensao = "")
	{
		if (tipoTensao == "" || !objetosTensao.Contains(tipoTensao))
		{
			objetosTensao.Add(tipoTensao);
			
			tensaoAtual = Mathf.Min(tensaoAtual + 1, maxTensao);
			
			if (tensaoAtual < tensaoSusto)
				DesativarBalaoReacao();

			tipoReacao = "tensao";

			if (tensaoAtual == tensaoSusto - 1)
				reacaoLiberada = true;

			AtualizarAnimacao("tensao", true, 1f); // Instanciar a duração da animação via inspector

			movimentar = false;

			/*if (tensaoAtual == 1)
				barraTensao.SetActive(true);*/

			AtualizarBarraTensao();
		}
	}
	
	public void ReduzirTensao(string tipoTensao = "")
	{
		/*if (objetosTensao.Contains(tipoTensao))
		{
			objetosTensao.Remove(tipoTensao);

			tensaoAtual = Mathf.Max(tensaoAtual - 1, 0);

			if (tensaoAtual == 0)
				barraTensao.SetActive(false);

			AtualizarBarraTensao();
		}*/
	}

	public void Atrair(GameObject objeto)
	{
		GameObject waypoint = null;
		foreach (Transform child in objeto.transform)
		{
			if (child.CompareTag("Waypoint"))
			{
				waypoint = child.gameObject;
				break;
			}
		}

		if (waypoint)
		{
			AdicionarDebugInfo("Atrair() - Alterar caminho para " + waypoint.name);
			caminho = Waypoint.DefinirCaminho(waypointAtual, waypoint, true);
			objetoInteracao = objeto;
			atualizarDirecaoAposMovimento = true;
		}
		else
			Debug.Log("Objeto: " + objeto.name);
	}

	public void Assustar()
	{
		if (!assustado && !chamarExorcista)
		{
			bool definirDestinoExorcista = false;

			if (jogadorScript.comandosHabilitados)
				jogadorScript.comandosHabilitados = false;

			bool liberarComandos = true;

			PararMovimento();

			assustado = true;
			AdicionarDebugInfo("AumentarTensão chamado dentro da função de Assustar - Direto.");
			AumentarTensao();
			
			StartCoroutine(TocarSom(jogo.gritos[Random.Range(0, jogo.gritos.Count)]));

			if (!chamarExorcista && tensaoAtual < tensaoSusto)
			{
				caminho = Waypoint.DefinirCaminho(waypointAtual, jogo.waypointInicial, true);
				AdicionarDebugInfo("Assustar() - Direto - Alterar caminho de " + waypointAtual.name + " para " + jogo.waypointInicial.name);
				chamarExorcista = true;
				definirDestinoExorcista = true;
				liberarComandos = false;

				jogo.exorcista.comodo = jogador.GetComponent<Personagem>().comodoAtual;
			}
			else
				StartCoroutine(Matar());
			
			foreach (KeyValuePair<GameObject, bool> chave in visaoNPCs)
			{
				GameObject npc = chave.Key;
				bool npcVisao = chave.Value;
				NPC npcScript = npc.GetComponent<NPC>();

				if (npcVisao && !npcScript.assustado)
				{
					npcScript.AdicionarDebugInfo("AumentarTensão chamado dentro da função de Assustar - Visão.");
					npcScript.AumentarTensao();
					if (!npcScript.chamarExorcista && !npcScript.assustado && npcScript.tensaoAtual < npcScript.tensaoSusto)
					{
						npcScript.PararMovimento();

						npcScript.assustado = true;

						npcScript.AdicionarDebugInfo("Assustar() - Visão - Alterar caminho para " + jogo.waypointInicial.name);
						
						npcScript.caminho = Waypoint.DefinirCaminho(npcScript.waypointAtual, jogo.waypointInicial, true);
						npcScript.chamarExorcista = true;
						definirDestinoExorcista = true;
						liberarComandos = false;

						jogo.exorcista.comodo = jogador.GetComponent<Personagem>().comodoAtual;
					}
				}
			}

			if (liberarComandos)
				jogadorScript.comandosHabilitados = true;

			if (definirDestinoExorcista)
			{
				if (jogadorScript.objetoInteracao)
					jogadorScript.EncerrarInteracao();

				exorcista.DefinirWaypointDestino();
			}
		}
	}

	public void SairChamandoExorcista()
	{
		if (!morto)
		{
			assustado = true;
			caminho = Waypoint.DefinirCaminho(waypointAtual, jogo.waypointInicial, true);
			exorcista.DefinirWaypointDestino();
			jogadorScript.comandosHabilitados = false;
			chamarExorcista = true;
		}
	}

	public void PararMovimento()
	{
		AdicionarDebugInfo("Parar movimento");
		iTween.Stop(transform.parent.gameObject);
		StopAllCoroutines();
		AtualizarAnimacao("movimento", false);
		emMovimento = false;
		movimentoLiberado = true;
	}

	IEnumerator Matar()
	{
		modificadorTensaoLiberado = false;
		yield return new WaitForSeconds(delayMorrer);
		//tipoReacao = "morto";
		//reacaoLiberada = true;
		//barraTensao.SetActive(false);
		DesativarBalaoReacao();
		AtualizarAnimacao("morto", true);
		morto = true;
	}

	IEnumerator NpcSaiu(bool aguardarDelay = true)
	{
		float delay = 0;
		if (aguardarDelay)
			delay = duracaoMovimento;

		yield return new WaitForSeconds(delay);
		AdicionarDebugInfo("Saiu no waypoint " + waypointAtual.name);
		StartCoroutine(EfetuarSaidaNpc());
	}

	IEnumerator PrepararExorcista()
	{
		yield return new WaitForSeconds(duracaoMovimento);
		jogo.exorcista.ChamarExorcista();
	}

	void AtualizarDirecao(Vector3 destino = default(Vector3))
	{
		if (destino != default(Vector3))
		{
			Vector3 diferenca = transform.position - destino;
			
			if (diferenca.x > 0.05f)
			{
				viradoDireita = false;
				exibirDirecao = "Esquerda";
			}
			else if(diferenca.x < -0.05f)
			{
				viradoDireita = true;
				exibirDirecao = "Direita";
			}

			if (diferenca.y > 0.05f)
			{
				if (direcao == 2)
				{
					cabelo.sprite = cabelos[0];
					camisa.sprite = camisas[0];
					bermuda.sprite = bermudas[0];
				}

				direcao = 1;
				exibirOrientacao = "Frente";
			}
			else if(diferenca.y < -0.05f)
			{
				if (direcao == 1)
				{
					cabelo.sprite = cabelos[1];
					camisa.sprite = camisas[1];
					bermuda.sprite = bermudas[1];
				}

				direcao = 2;
				exibirOrientacao = "Costas";
			}
		}

		int sortingOrderCamera = 1;
		int sortingOrderBracos = 2;
		int sortingOrderFlash = 3;

		if (direcao == 2)
		{
			sortingOrderCamera *= -1;
			sortingOrderBracos *= -1;
			sortingOrderFlash *= -1;
		}

		transform.FindChild("Câmera").GetComponent<OrdenarCenario>().orderInicial = sortingOrderCamera;
		transform.FindChild("Braços").GetComponent<OrdenarCenario>().orderInicial = sortingOrderBracos;
		transform.FindChild("Flash").GetComponent<OrdenarCenario>().orderInicial = sortingOrderFlash;

		bool direita = Mathf.Sign(transform.localScale.x) == 1f;
		float scaleX = transform.localScale.x;

		if ((viradoDireita && !direita) || (!viradoDireita && direita))
			scaleX = scaleX * -1f;

		transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);

		Vector3 camisaScale = transform.FindChild("Camisa").localScale;
		transform.FindChild("Camisa").localScale = new Vector3(Mathf.Sign(scaleX), camisaScale.y, camisaScale.z);

		LimparAnimacoes();
		animator.SetInteger("direcao", direcao);
	}

	void AtualizarAnimacao(string animacao, bool valor)
	{
		LimparAnimacoes();
		animator.SetBool(animacao, valor);
	}

	IEnumerator AtualizarAnimacao(string animacao, bool valor, float delay)
	{
		AtualizarAnimacao(animacao, valor);
		yield return new WaitForSeconds(delay);
		LimparAnimacoes("foto");
	}

	void LimparAnimacoes(string animacao = "")
	{
		if (animacao == "")
		{
			animator.SetBool("movimento", false);
			animator.SetBool("foto", false);
		}
		else
			animator.SetBool(animacao, false);
	}
	
	IEnumerator TocarSom(AudioClip som)
	{
		audioSource.clip = som;
		yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
		audioSource.Play();
	}

	public void AdicionarDebugInfo(string texto)
	{
		debugInfo.Add(transform.parent.name + ": " + texto);
	}

	IEnumerator EfetuarSaidaNpc()
	{
		foreach (Transform child in transform)
			child.gameObject.SetActive(false);

		DesativarBalaoReacao();

		while (audioSource.volume > 0f)
		{
			audioSource.volume -= 0.05f;
			yield return new WaitForSeconds(0.1f);
		}

		Destroy(transform.parent.gameObject);
		jogo.npcsEmJogo--;
		jogo.quantidadeNpcsSairam++;
	}
}
