using UnityEngine;
using System.Collections.Generic;


public class Emoticon : MonoBehaviour
{
	public List<Sprite> emoticons = new List<Sprite>();

	[Header("Emoticon Selecionado")]
	[Range(0, 30)]
	public int emoticonId;
	public string emoticonNome;

	[Header("Reações")]
	public List<string> reacoesObjetos;
	public List<string> reacoesTensao;
	public List<string> reacoesMovimento;
	public List<string> reacoesMorto;

	/*void OnDrawGizmosSelected()
	{
		Sprite sprite = null;
		int id = 0;

		if (emoticonNome != "")
		{
			foreach (Sprite emoticon in emoticons)
			{
				if (emoticonNome == emoticon.name)
				{
					sprite = emoticon;
					break;
				}
			}
		}
		
		if (sprite == null)
		{
			if (emoticonId >= 0 && emoticonId < emoticons.Count)
				id = emoticonId;
			
			sprite = emoticons[id];
		}
		
		GetComponent<SpriteRenderer>().sprite = sprite;
	}*/

	public void ReacaoObjeto()
	{
		AplicarEmoticon(reacoesObjetos[Random.Range(0, reacoesObjetos.Count)]);
	}

	public void ReacaoTensao()
	{
		AplicarEmoticon(reacoesTensao[Random.Range(0, reacoesTensao.Count)]);
	}

	public void ReacaoMovimento()
	{
		AplicarEmoticon(reacoesMovimento[Random.Range(0, reacoesMovimento.Count)]);
	}

	public void ReacaoMorto()
	{
		AplicarEmoticon(reacoesMorto[Random.Range(0, reacoesMorto.Count)]);
	}

	void AplicarEmoticon(string nome)
	{
		Sprite sprite = null;

		foreach (Sprite emoticon in emoticons)
		{
			if (nome == emoticon.name)
			{
				sprite = emoticon;
				break;
			}
		}

		if (sprite)
			GetComponent<SpriteRenderer>().sprite = sprite;
	}
}
