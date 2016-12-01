using UnityEngine;
using System.Collections;

public class Objeto : MonoBehaviour
{
	[Header("Chamar Atenção")]
	public bool chamarAtencao;
	public float delayChamarAtencao;
	public float duracaoChamarAtencao;
	public bool encerrarChamarAtencao;
	public float delayEncerrarChamarAtencao;
	private float tempoEncerrarChamarAtencao;
	public AudioClip somChamarAtencao;
	public bool somChamarAtencaoLoop;
	internal bool chamarAtencaoLiberado = true;
	internal bool chamandoAtencao;

	[Header("Assustar")]
	public bool assustar;
	public float delayAssustar;
	public AudioClip somAssustar;
	internal float ultimoAssustar;
	public float duracaoAssustar;
	internal bool assustando;

	[Header("Waypoint")]
	public bool instanciarWaypoint;
	public Vector2 posicaoWaypoint;
	public Vector2 variacaoWaypoint = default(Vector2);
	private GameObject waypointObjeto;

	[Header("Borda")]
	public Color corBordaAtiva = new Vector4(0f, 1f, 1f, 1f);
	public Color corBordaInativa = new Vector4(1f, 0f, 0f, 1f);

	private Jogador jogador;
	private Jogo jogo;
	private Animator animator;
	private SpriteRenderer spriteRenderer;
	private AudioSource audioSource;
	private bool reproduzindoAudio;

	private bool bordaPiscando;
	private GameObject bordaObjeto;
	private SpriteRenderer bordaObjetoSpriteRenderer;

	void Start()
	{
		if ((chamarAtencao || assustar) && !instanciarWaypoint)
		{
			instanciarWaypoint = true;
			Debug.LogError("Objeto " + gameObject.name + " não está instanciando um waypoint.");
		}

		jogador = Jogador.PegarControlador();
		jogo = Jogo.Pegar();
		spriteRenderer = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();

		if (chamarAtencao)
		{
			bordaObjeto = Instantiate(jogo.bordaObjetos, transform.position, transform.localRotation) as GameObject;
			bordaObjeto.name = "Borda";
			bordaObjeto.transform.parent = transform;
			bordaObjeto.transform.localScale = new Vector3(1f, 1f, 1f);
			bordaObjetoSpriteRenderer = bordaObjeto.GetComponent<SpriteRenderer>();
			AlterarCorBorda();

			foreach (Sprite sprite in jogo.spritesBordaObjetos)
			{
				if (sprite.name == spriteRenderer.sprite.name || sprite.name == spriteRenderer.sprite.name + " - Borda")
				{
					bordaObjetoSpriteRenderer.sprite = sprite;
					break;
				}
			}
		}

		if (somAssustar || somChamarAtencao)
		{
			if (gameObject.GetComponent<AudioSource>())
				audioSource = gameObject.GetComponent<AudioSource>();
			else
				audioSource = gameObject.AddComponent<AudioSource>();
		}

		if (instanciarWaypoint)
		{
			waypointObjeto = Instantiate<GameObject>(jogo.waypointObjeto);
			waypointObjeto.transform.parent = gameObject.transform;
			waypointObjeto.transform.localPosition = posicaoWaypoint;

			Waypoint waypointScript = waypointObjeto.GetComponent<Waypoint>();
			if (variacaoWaypoint != default(Vector2))
			{
				waypointScript.variarPosicao = true;
				waypointScript.variacao = variacaoWaypoint;
			}
		}
	}

	void Update()
	{
		if (jogador.objetoInteracao == gameObject && bordaPiscando)
			AlterarBorda(false);
		
		if (audioSource && !audioSource.isPlaying)
			reproduzindoAudio = false;
	}

	void OnTriggerStay2D(Collider2D coll)
	{
		if (coll.CompareTag("Player"))
		{
			AlterarBorda(true);

			coll.GetComponent<Jogador>().objetoMarcado = gameObject;
		}
	}

	void OnTriggerExit2D(Collider2D coll)
	{
		if (coll.CompareTag("Player"))
		{
			AlterarBorda(false);

			if (coll.GetComponent<Jogador>().objetoMarcado == gameObject)
				coll.GetComponent<Jogador>().objetoMarcado = null;
		}
	}

	public void AlterarBorda(bool ativa)
	{
		if (bordaPiscando != ativa)
		{
			bordaPiscando = ativa;
			Transform bordaObjeto = transform.FindChild("Borda");
			if (bordaObjeto)
				bordaObjeto.GetComponent<Animator>().SetBool("piscar", ativa);
		}
	}

	void AlterarCorBorda()
	{
		if (chamarAtencaoLiberado || (!assustar && chamarAtencaoLiberado) || (assustar && (Time.time < ultimoAssustar + delayAssustar)))
			bordaObjetoSpriteRenderer.color = corBordaAtiva;
		else
			bordaObjetoSpriteRenderer.color = corBordaInativa;
	}

	public void ChamarAtencao()
	{
		if (chamarAtencaoLiberado)
		{
			chamarAtencaoLiberado = false;
			
			AlterarCorBorda();

			if (animator)
			{
				animator.SetBool("assustar", false);
				animator.SetBool("chamarAtencao", true);
			}

			if (audioSource)
			{
				audioSource.clip = somChamarAtencao;

				if (somChamarAtencaoLoop)
					audioSource.loop = true;

				reproduzindoAudio = true;
				audioSource.Play();

				//AudioSource.PlayClipAtPoint(somChamarAtencao, transform.position);
			}
			
			GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
			foreach (GameObject npc in npcs)
			{
				GameObject comodoJogador = Personagem.PegarControlador("Jogador").comodoAtual;
				GameObject comodoNpc = Personagem.PegarControlador(npc.transform.parent.name, true).comodoAtual;
				if (comodoJogador == comodoNpc)
				{
					NPC npcScript = npc.GetComponent<NPC>();
					npcScript.DesativarBalaoReacao();
					npcScript.reacaoLiberada = false;
					npcScript.tipoReacao = "tensao";
					npcScript.objetosVisitados.Add(gameObject);
					npcScript.Atrair(gameObject);
					npcScript.AumentarTensao(gameObject.name);
				}
			}

			chamandoAtencao = true;
			tempoEncerrarChamarAtencao = Time.time;

			StartCoroutine(EncerrarChamarAtencao(duracaoChamarAtencao));
		}
	}

	void OnBecameVisible()
	{
		if (audioSource && reproduzindoAudio)
			audioSource.Play();
	}

	void OnBecameInvisible()
	{
		if (audioSource && reproduzindoAudio)
			audioSource.Stop();
	}

	public void EncerrarChamarAtencao()
	{
		if (encerrarChamarAtencao && Time.time >= tempoEncerrarChamarAtencao + delayEncerrarChamarAtencao)
			StartCoroutine(EncerrarChamarAtencao(0));
	}

	IEnumerator EncerrarChamarAtencao(float delay)
	{
		yield return new WaitForSeconds(delay);

		if (chamandoAtencao)
			StartCoroutine(LiberarChamarAtencao());
	}

	IEnumerator LiberarChamarAtencao()
	{
		chamandoAtencao = false;

		if (animator)
			animator.SetBool("chamarAtencao", false);

		if (audioSource)
			audioSource.Stop();

		GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
		foreach (GameObject npc in npcs)
		{
			NPC npcScript = npc.GetComponent<NPC>();
			if (npcScript.objetoInteracao == gameObject)
			{
				npcScript.SairChamandoExorcista();
			}
		}

		yield return new WaitForSeconds(delayChamarAtencao);
		chamarAtencaoLiberado = true;
		AlterarCorBorda();
	}

	public void Assustar()
	{
		if (!assustando && (ultimoAssustar == 0 || Time.time > ultimoAssustar + delayAssustar))
		{
			assustando = true;

			AlterarCorBorda();

			if (animator)
			{
				animator.SetBool("assustar", true);
				animator.SetBool("chamarAtencao", false);
			}

			if (audioSource)
			{
				audioSource.clip = somAssustar;
				audioSource.loop = false;
				audioSource.Play();
			}

			GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
			foreach (GameObject npc in npcs)
			{
				NPC npcScript = npc.GetComponent<NPC>();
				if (gameObject == npcScript.objetoInteracao)
					npcScript.Assustar();
			}

			ultimoAssustar = Time.time;

			StartCoroutine(EncerrarAssustar(duracaoAssustar));
		}
	}

	IEnumerator EncerrarAssustar(float delay)
	{
		yield return new WaitForSeconds(delay);

		if (assustando)
		{
			assustando = false;

			if (animator)
				animator.SetBool("assustar", false);
		}
	}

	public void LimparAnimacoes()
	{
		if (animator)
		{
			animator.SetBool("assustar", false);
			animator.SetBool("chamarAtencao", false);
		}

		if (audioSource)
		{
			audioSource.Stop();
			reproduzindoAudio = false;
		}
	}
}
