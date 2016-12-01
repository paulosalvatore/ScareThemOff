using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Waypoint : MonoBehaviour
{
	[Header("Caminho")]
	public List<GameObject> waypointsDisponiveis = new List<GameObject>();
	public Color pathColor = Color.cyan;

	[Header("Variação")]
	public Vector2 variacao = default(Vector2);
	public bool variarPosicao;

	[Header("Referências")]
	public bool travarMovimento;
	public bool movimentoAutomatico;
	public bool objeto;

	internal GameObject comodo;

	private Jogo jogo;

	void Awake()
	{
		ChecarLigacoes();
	}

	void Start()
	{
		jogo = Jogo.Pegar();
		jogo.waypointId++;
		gameObject.name = "Waypoint (" + jogo.waypointId + ")";
		DefinirComodoAtual();

		if (objeto)
		{
			List<GameObject> waypoints = Waypoint.PegarWaypoints(comodo, true);

			Dictionary<GameObject, float> waypointsDistancia = new Dictionary<GameObject, float>();

			foreach (GameObject waypoint in waypoints)
			{
				bool checarWaypoint = !Physics2D.Linecast(transform.position, waypoint.transform.position, 1 << LayerMask.NameToLayer("Parede"));
				if (checarWaypoint)
					waypointsDistancia.Add(waypoint, Vector3.Distance(waypoint.transform.position, transform.position));
			}

			if (waypointsDistancia.Count() > 0)
			{
				var waypointsDistanciaOrdenados = from pair in waypointsDistancia
												  orderby pair.Value ascending
												  select pair;

				GameObject waypointMaisProximo = waypointsDistanciaOrdenados.First().Key;

				waypointMaisProximo.GetComponent<Waypoint>().waypointsDisponiveis.Add(gameObject);
				waypointsDisponiveis.Add(waypointMaisProximo);
			}
		}
	}

	void OnDrawGizmosSelected()
	{
		if (waypointsDisponiveis.Count > 0)
		{
			foreach (GameObject waypoint in waypointsDisponiveis)
			{
				/*
				List<Vector3> list = new List<Vector3>();
				list.Add(transform.position);
				list.Add(waypoint.transform.position);
				iTween.DrawPath(list.ToArray(), pathColor);
				*/
				Gizmos.color = pathColor;
				Gizmos.DrawLine(transform.position, waypoint.transform.position);
			}
		}
	}

	void DefinirComodoAtual()
	{
		GameObject[] comodos = GameObject.FindGameObjectsWithTag("Comodo");

		foreach (GameObject checarComodo in comodos)
		{
			PolygonCollider2D comodoCollider = checarComodo.GetComponent<PolygonCollider2D>();
			bool checarColisao = comodoCollider.OverlapPoint(transform.position);

			if (checarColisao && checarComodo != comodo)
				comodo = checarComodo;
		}
	}

	void ChecarLigacoes()
	{
		foreach(GameObject waypointDisponivel in waypointsDisponiveis)
		{
			List<GameObject> checarWaipointsDisponiveis = waypointDisponivel.GetComponent<Waypoint>().waypointsDisponiveis;
			if (!checarWaipointsDisponiveis.Contains(gameObject))
				checarWaipointsDisponiveis.Add(gameObject);
		}
	}

	static public List<GameObject> PegarWaypoints(GameObject comodo = null, bool excluirObjetos = false)
	{
		GameObject[] waypointPrincipal = GameObject.FindGameObjectsWithTag("Waypoint");

		List<GameObject> waypoints = new List<GameObject>();

		foreach (GameObject child in waypointPrincipal)
		{
			bool adicionar = true;

			if (!child.GetComponent<Waypoint>())
				Debug.Log(child.name);

			if (excluirObjetos && child.GetComponent<Waypoint>().objeto)
				adicionar = false;

			if (comodo && comodo != child.GetComponent<Waypoint>().comodo)
				adicionar = false;

			if (adicionar)
				waypoints.Add(child);
		}
		
		return waypoints;
	}
	
	static public List<GameObject> DefinirCaminho(GameObject waypointInicial, GameObject waypointDestino = null, bool removerPrimeiroElemento = false)
	{
		List<GameObject> caminho = new List<GameObject>();

		List<GameObject> waypoints = Waypoint.PegarWaypoints();

		if (!waypointDestino)
		{
			Personagem personagem = Personagem.PegarControlador("Jogador");

			foreach (GameObject waypoint in waypoints)
				if (waypoint.GetComponent<Waypoint>().comodo == personagem.comodoAtual)
					waypointDestino = waypoint;
		}

		if (waypointDestino)
		{
			Dictionary<string, List<GameObject>> waypointsDisponiveis = new Dictionary<string, List<GameObject>>();
			foreach (GameObject waypoint in waypoints)
				if (waypoint.GetComponent<Waypoint>())
					waypointsDisponiveis.Add(waypoint.name, waypoint.GetComponent<Waypoint>().waypointsDisponiveis);

			List<string> verificados = new List<string>();
			List<GameObject> waypointsChecar = new List<GameObject>();
			waypointsChecar.Add(waypointInicial);

			Dictionary<string, List<GameObject>> caminhos = new Dictionary<string, List<GameObject>>();
			while (waypointsChecar.Count > 0)
			{
				int keyChecar = waypointsChecar.Count - 1;
				GameObject checagemAtual = waypointsChecar[keyChecar];

				List<GameObject> caminhoAtual = new List<GameObject>();

				if (checagemAtual == waypointInicial)
				{
					caminhoAtual.Add(checagemAtual);
					caminhos.Add(checagemAtual.name, caminhoAtual);
				}
				else
					foreach (GameObject item in caminhos[checagemAtual.name])
						caminhoAtual.Add(item);

				waypointsChecar.RemoveAt(keyChecar);
				verificados.Add(checagemAtual.name);

				List<GameObject> listaDisponiveis = waypointsDisponiveis[checagemAtual.name];
				foreach (GameObject waypointDisponivel in listaDisponiveis)
				{
					List<GameObject> caminhoClonar = new List<GameObject>();
					foreach (GameObject item in caminhoAtual)
						caminhoClonar.Add(item);

					caminhoClonar.Add(waypointDisponivel);

					if (!verificados.Contains(waypointDisponivel.name) && !caminhos.ContainsKey(waypointDisponivel.name))
					{
						caminhos.Add(waypointDisponivel.name, caminhoClonar);
						waypointsChecar.Add(waypointDisponivel);
					}
				}
			}

			if (caminhos.ContainsKey(waypointDestino.name))
				caminho = caminhos[waypointDestino.name];
			else
				Debug.Log("Um erro ocorrer ao tentar definir um caminho de '" + waypointInicial.name + "' para " + waypointDestino.name + "'.");
		}

		if (removerPrimeiroElemento && caminho.Count > 1)
			caminho.RemoveAt(0);

		return caminho;
	}
}
