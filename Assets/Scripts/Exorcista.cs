using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Exorcista : MonoBehaviour
{
	public GameObject waypointInicial;
	private Waypoint waypointInicialScript;

	private Jogo jogo;
	private Personagem jogador;
	private Jogador jogadorScript;
	private Animator animator;
	private SpriteRenderer spriteRenderer;
	private AudioSource audioSource;

	private bool chamarExorcista;
	internal bool exorcistaChamado;
	private List<GameObject> caminho;
	internal GameObject comodo;

	private bool movimentar;
	private int waypointAtual;
	internal GameObject waypointDestino;
	private bool voltando;
	private bool viradoDireita;

	public float duracaoBenzer;

	void Start()
	{
		jogo = Jogo.Pegar();
		jogador = Personagem.PegarControlador("Jogador");
		jogadorScript = Jogador.PegarControlador();
		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		audioSource = GetComponent<AudioSource>();
		spriteRenderer.enabled = false;
	}

	void Update ()
	{
		if (chamarExorcista)
		{
			animator.SetBool("emMovimento", true);
			chamarExorcista = false;
			IniciarExorcista();
			exorcistaChamado = true;
		}
	}

	void Mover()
	{
		if (waypointAtual < caminho.Count)
		{
			Vector3 posicao = caminho[waypointAtual].transform.position;

			int direcao = 0;
			Vector3 diferenca = transform.position - posicao;

			if (diferenca.x > 0.05f)
				viradoDireita = false;
			else if (diferenca.x < -0.05f)
				viradoDireita = true;

			if (diferenca.y > 0.05f)
				direcao = 1;
			else if (diferenca.y < -0.05f)
				direcao = 2;

			bool direita = !spriteRenderer.flipX;

			if ((viradoDireita && !direita) || (!viradoDireita && direita))
				spriteRenderer.flipX = !spriteRenderer.flipX;

			animator.SetInteger("direcao", direcao);

			float duracaoMovimento = Vector3.Distance(transform.position, posicao) / 1.3f;

			iTween.MoveTo(gameObject, iTween.Hash("position", posicao, "easeType", iTween.EaseType.linear, "time", duracaoMovimento, "onComplete", "Mover"));
			waypointAtual++;
		}
		else if (!voltando)
		{
			StartCoroutine(ExorcisarComodo());
		}
		else
		{
			chamarExorcista = false;
			exorcistaChamado = false;
			voltando = false;
			spriteRenderer.enabled = false;
			jogadorScript.comandosHabilitados = true;
		}
	}

	IEnumerator ExorcisarComodo()
	{
		animator.SetBool("emMovimento", false);
		animator.SetBool("exorcisando", true);

		Comodo comodo = waypointDestino.GetComponent<Waypoint>().comodo.GetComponent<Comodo>();
		comodo.BloquearComodo();
		if (comodo.luzApagada)
			jogo.AlterarLuzComodo(comodo.gameObject.transform.FindChild("Luz").GetComponent<MeshRenderer>());

		comodo.tempoComodoBloqueado = Time.time;

		audioSource.Play();

		yield return new WaitForSeconds(duracaoBenzer);

		animator.SetBool("emMovimento", true);
		animator.SetBool("exorcisando", false);

		caminho.Reverse();
		waypointAtual = 1;
		voltando = true;
		Mover();
	}

	public void ChamarExorcista()
	{
		if (!exorcistaChamado && !chamarExorcista)
		{
			spriteRenderer.enabled = true;
			chamarExorcista = true;
		}
	}

	void IniciarExorcista()
	{
		transform.position = waypointInicial.transform.position;
		
		caminho = Waypoint.DefinirCaminho(waypointInicial, waypointDestino);

		waypointDestino = caminho[caminho.Count - 1];

		waypointAtual = 1;

		Mover();
	}

	public void DefinirWaypointDestino()
	{
		comodo = jogador.comodoAtual;

		List<GameObject> waypoints = Waypoint.PegarWaypoints(comodo, true);

		Dictionary<GameObject, float> waypointsDistancia = new Dictionary<GameObject, float>();

		Vector3 posicao = jogador.transform.position;

		foreach (GameObject waypoint in waypoints)
			waypointsDistancia.Add(waypoint, Vector3.Distance(posicao, waypoint.transform.position));

		var waypointsDistanciaOrdenados = from pair in waypointsDistancia
										  orderby pair.Value ascending
										  select pair;

		var waypointsProximos = waypointsDistanciaOrdenados.ToList();

		waypointDestino = waypointsProximos.First().Key;
	}

	public static GameObject PegarObjeto()
	{
		return GameObject.Find("Exorcista");
	}

	public static Exorcista PegarControlador()
	{
		return PegarObjeto().GetComponent<Exorcista>();
	}
}
