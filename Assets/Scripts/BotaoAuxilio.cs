using UnityEngine;
using UnityEngine.UI;

public class BotaoAuxilio : MonoBehaviour
{
	public Sprite imagemAtivo;
	public Sprite imagemInativo;

	private Image imagem;

	void Start()
	{
		imagem = GetComponent<Image>();
	}

	public void AlterarImagem(bool ativo)
	{
		imagem.sprite = ativo ? imagemAtivo : imagemInativo;
	}
}
