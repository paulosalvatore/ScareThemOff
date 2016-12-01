using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using XInputDotNetPure;

public class Jogador : MonoBehaviour
{
	[Header("Movimento")]
	public float velocidade;

	[Header("Vida")]
	public float vida;
	public Image barraVida;
	private float vidaMaxima;
	private float invulneravel;
	private float delayInvulneravel = 5f;

	internal GameObject objetoMarcado;
	internal GameObject objetoInteracao;

	internal bool comandosComunsHabilitados = true;
	internal bool comandosHabilitados = true;

	private new Rigidbody2D rigidbody;
	internal Animator animator;
	private Personagem personagem;
	private Jogo jogo;
	private SpriteRenderer spriteRenderer;
	internal GameObject fantasma;

	private int direcao;
	private bool viradoDireita;

	[Header("Susto")]
	public float delaySusto;
	public float duracaoAnimacaoSusto;
	private bool sustoLiberado = true;
	private GameObject comodoSusto;

	[Header("Vibração")]
	public Vector2 forcaVibracao;
	public int quantidadeVibracoes;
	public float duracaoVibracao;
	public float intervaloEntreVibracoes;
	public float delayVibracao;
	public float delayInicioVibracao;
	private bool vibrarControle;

	void Start()
	{
		jogo = Jogo.Pegar();

		rigidbody = GetComponent<Rigidbody2D>();

		fantasma = transform.FindChild("Fantasma").gameObject;
		animator = fantasma.GetComponent<Animator>();
		spriteRenderer = fantasma.GetComponent<SpriteRenderer>();
		personagem = Personagem.PegarControlador("Jogador");
		vidaMaxima = vida;

		StartCoroutine(ChecarInvulneravel());
	}

	void Update()
	{
		if (jogo.vitoria)
			return;

		if (jogo.controladorCena.jogoPausado)
			return;

		ChecarTensaoNpcsComodo();

		if (personagem.comodoAtual && personagem.comodoAtual.GetComponent<Comodo>().ChecarComodoBloqueado())
		{
			if (vida > 0)
			{
				if (Time.time > invulneravel)
				{
					vida--;
					if (vida > 0)
						invulneravel = Time.time + delayInvulneravel;
					jogo.AtualizarVidas();
				}
			}
			else
			{
				jogo.controladorCena.CarregarCena("gameover");
			}
		}
	}

	IEnumerator ChecarInvulneravel()
	{
		while (true)
		{
			if (Time.time < invulneravel)
			{
				spriteRenderer.color = new Vector4(0, 0, 0, 1);
				yield return new WaitForSeconds(0.2f);
				spriteRenderer.color = new Vector4(1, 1, 1, 1);
			}
			yield return new WaitForSeconds(0.4f);
		}
	}

	void LateUpdate()
	{
		if (jogo.controladorCena.jogoPausado)
			return;

		float h = 0;
		float v = 0;
		
		if (comandosComunsHabilitados)
		{
			h = Input.GetAxis("Horizontal") * velocidade;
			v = Input.GetAxis("Vertical") * velocidade;
		}
		
		rigidbody.velocity = new Vector2(h, v);

		if (Mathf.Abs(h) > 0 || Mathf.Abs(v) > 0)
		{
			if (h < 0)
				viradoDireita = false;
			else if (h > 0)
				viradoDireita = true;

			AtualizarDirecao();
			AtualizarAnimacao("movimento", true);

			jogo.AtualizarPosicaoCameraJogador();
		}
		else
			AtualizarAnimacao("movimento", false);
	}

	public static GameObject PegarObjeto()
	{
		return GameObject.Find("Jogador");
	}

	public static Jogador PegarControlador()
	{
		return PegarObjeto().GetComponent<Jogador>();
	}

	public void IniciarInteracao()
	{
		animator.SetBool("interacaoObjeto", true);
		objetoInteracao = objetoMarcado;
		objetoInteracao.GetComponent<Objeto>().AlterarBorda(false);
		comandosComunsHabilitados = false;
	}

	public void EncerrarInteracao()
	{
		animator.SetBool("interacaoObjeto", false);
		//objetoInteracao.GetComponent<Objeto>().LimparAnimacoes(); // Testando, deixar a TV chamando interação mesmo sem o jogador - Só desligar depois do tempo dela
		objetoInteracao = null;
		comandosComunsHabilitados = true;
	}

	public void Assustar()
	{
		if (sustoLiberado)
		{
			comodoSusto = personagem.comodoAtual;
			sustoLiberado = false;
			StartCoroutine(LiberarSusto());
			StartCoroutine(AtualizarAnimacao("assustar", true, duracaoAnimacaoSusto));
			
			GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
			foreach (GameObject npc in npcs)
			{
				NPC npcScript = npc.GetComponent<NPC>();
				Personagem npcPersonagem = Personagem.PegarControlador(npc.transform.parent.name, true);
				if (npcScript.visaoJogador)
					npcScript.Assustar();
			}
		}
	}

	IEnumerator LiberarSusto()
	{
		yield return new WaitForSeconds(delaySusto);
		sustoLiberado = true;
}

	void AtualizarDirecao(Vector3 destino = default(Vector3))
	{
		AtualizarAnimacao("direcao", viradoDireita == true ? 2 : 1);
	}

	void AtualizarAnimacao(string animacao, int valor)
	{
		LimparAnimacoes();
		animator.SetInteger(animacao, valor);
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
		LimparAnimacoes(animacao);
	}

	void LimparAnimacoes(string animacao = "")
	{
		if (jogo.vitoria)
			return;

		if (animacao == "")
		{
			animator.SetBool("movimento", false);
		}
		else
			animator.SetBool(animacao, false);
	}
	
	void ChecarTensaoNpcsComodo()
	{
		bool npcTensaoSusto = false;
		GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
		foreach (GameObject npc in npcs)
		{
			NPC npcScript = npc.GetComponent<NPC>();
			Personagem npcPersonagem = Personagem.PegarControlador(npc.transform.parent.name, true);
			if (npcPersonagem.comodoAtual == personagem.comodoAtual)
				if (!npcScript.morto && !npcScript.assustado && !npcScript.chamarExorcista && npcScript.visaoJogador && npcScript.tensaoAtual >= npcScript.tensaoSusto - 1)
					npcTensaoSusto = true;
		}

		if (npcTensaoSusto && !vibrarControle	)
		{
			vibrarControle = true;

			StartCoroutine(VibrarControle());

			if (!jogo.audioSourceEfeitos.isPlaying)
			{
				jogo.audioSourceEfeitos.clip = jogo.coracaoBatendo;
				jogo.audioSourceEfeitos.Play();
			}
			npcTensaoSusto = false;
		}
		else if (!npcTensaoSusto && vibrarControle)
		{
			vibrarControle = false;

			if (jogo.audioSourceEfeitos.isPlaying)
				jogo.audioSourceEfeitos.Stop();
		}
	}

	IEnumerator VibrarControle()
	{
		yield return new WaitForSeconds(delayInicioVibracao);
		while (true)
		{
			if (vibrarControle)
			{
				for (int i = 0; i < quantidadeVibracoes; i++)
				{
					GamePad.SetVibration(jogo.controladorCena.playerIndex, forcaVibracao.x, forcaVibracao.y);
					yield return new WaitForSeconds(duracaoVibracao);
					GamePad.SetVibration(jogo.controladorCena.playerIndex, 0f, 0f);
					yield return new WaitForSeconds(intervaloEntreVibracoes);
				}
			}
			else
			{
				GamePad.SetVibration(jogo.controladorCena.playerIndex, 0f, 0f);
				break;
			}

			yield return new WaitForSeconds(delayVibracao);
		}
	}

}
