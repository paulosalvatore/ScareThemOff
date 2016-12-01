using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
	using UnityEditor;
#endif

public class Comodo : MonoBehaviour
{
	[DisplayWithoutEdit()] public bool luzApagada;

	public bool luzControlavel;

	public bool limiteNpcs;

	public bool chamarNpcs;
	private bool npcChamado;

	private GameObject luz;
	private GameObject bloqueio;
	private Jogo jogo;

	internal List<GameObject> objetosComodo = new List<GameObject>();
	internal List<GameObject> npcsComodo = new List<GameObject>();

	private float tempoLuzApagada;

	internal float tempoComodoBloqueado;

	void Start()
	{
		jogo = Jogo.Pegar();

		luz = transform.FindChild("Luz").gameObject;
		luz.GetComponent<MeshRenderer>().sortingLayerName = "TopLayer";

		bloqueio = transform.FindChild("Bloqueio").gameObject;
		bloqueio.SetActive(false);
		bloqueio.GetComponent<MeshRenderer>().sortingLayerName = "TopLayer";

		PolygonCollider2D comodoCollider = GetComponent<PolygonCollider2D>();
		GameObject[] objetos = GameObject.FindGameObjectsWithTag("Objeto");
		foreach (GameObject objeto in objetos)
		{
			Objeto objetoScript = objeto.GetComponent<Objeto>();
			if (objetoScript && objetoScript.instanciarWaypoint && comodoCollider.OverlapPoint(objeto.transform.position))
				objetosComodo.Add(objeto);
		}

		if (chamarNpcs)
			StartCoroutine(ChamadaNpcs());
	}

	void Update()
	{
		ChecarLuzComodo();
		ChecarBloqueioComodo();
	}

	IEnumerator ChamadaNpcs()
	{
		while (true)
		{
			Delay delay = jogo.delayChamadaNpcs;

			if (npcChamado)
				delay = jogo.delayComodoChamarNpcs;

			yield return new WaitForSeconds(Random.Range(delay.min, delay.max));

			if (npcChamado && npcsComodo.Count == 0)
				npcChamado = false;
			else if (!npcChamado)
			{
				if (npcsComodo.Count == 0)
				{
					float chance = Random.Range(1, 100);
					if (chance < jogo.chanceNpcChamadaComodo)
					{
						GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");

						List<GameObject> npcsDisponiveis = new List<GameObject>();

						foreach (GameObject npc in npcs)
						{
							GameObject comodoNpc = npc.GetComponent<Personagem>().comodoAtual;
							if (!comodoNpc || comodoNpc.name == "Corredor")
								npcsDisponiveis.Add(npc);
						}
						
						if (npcs.Count() > 0)
						{
							GameObject npcSelecionado = npcs.PickRandom();
							npcSelecionado.GetComponent<NPC>().chamadoComodo = gameObject;
							npcChamado = true;
						}
					}
				}
			}
		}
	}

	void OnTriggerStay2D(Collider2D coll)
	{
		if (coll.CompareTag("Player"))
		{
			Personagem personagem = Personagem.PegarControlador(coll.gameObject.name);
			if (!personagem.PegarComodoAtual())
				personagem.DefinirComodoAtual(gameObject);
		}
	}

	void OnTriggerExit2D(Collider2D coll)
	{
		if (coll.CompareTag("Player"))
		{
			Personagem personagem = Personagem.PegarControlador(coll.gameObject.name);
			personagem.DefinirComodoAtual(null);
		}
	}

	static public List<GameObject> PegarComodos()
	{
		return GameObject.FindGameObjectsWithTag("Comodo").ToList();
	}

	public MeshRenderer PegarLuz()
	{
		if (luzControlavel)
			return transform.FindChild("Luz").GetComponent<MeshRenderer>();

		return null;
	}

	public void AlterarLuz(bool luzApagada)
	{
		bool npcComodo = false;
		GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
		foreach(GameObject npc in npcs)
		{
			Personagem personagem = npc.GetComponent<Personagem>();
			NPC npcScript2 = npc.GetComponent<NPC>();
			npcScript2.AdicionarDebugInfo("AlterarLuz() - Checar se o comodo do NPC é igual ao comodo em questão");
			npcScript2.AdicionarDebugInfo("Cômodo NPC: " + personagem.comodoAtual.name);
			npcScript2.AdicionarDebugInfo("Cômodo em Questão: " + gameObject.name);
			npcScript2.AdicionarDebugInfo("(personagem.comodoAtual == gameObject): " + (personagem.comodoAtual == gameObject));
			if (personagem.comodoAtual == gameObject)
			{
				npcComodo = true;

				NPC npcScript = npc.GetComponent<NPC>();
				npcScript.AdicionarDebugInfo("AlterarLuz() - Checar se a Luz está apagada ou não para aumentar a tensão.");
				if (luzApagada)
					npcScript.AumentarTensao("Luz - " + gameObject.name);
				else
					npcScript.ReduzirTensao("Luz - " + gameObject.name);
			}
		}

		float delay = jogo.oscilarDuracao;
		if (npcComodo)
			delay += jogo.delayChecarLuzComodo;

		tempoLuzApagada = Time.time + delay;
	}

	void ChecarLuzComodo()
	{
		if (Time.time > tempoLuzApagada)
			luzApagada = false;
	}

	public void BloquearComodo()
	{
		bloqueio.SetActive(true);
	}

	public void DesbloquearComodo()
	{
		bloqueio.SetActive(false);
	}

	public bool ChecarComodoBloqueado()
	{
		return bloqueio.activeSelf;
	}

	void ChecarBloqueioComodo()
	{
		if (ChecarComodoBloqueado() && Time.time > tempoComodoBloqueado + jogo.delayComodoExorcisado	)
			DesbloquearComodo();
	}
}