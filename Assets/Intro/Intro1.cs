using UnityEngine;
using System.Collections;
using XInputDotNetPure;

public class Intro1 : MonoBehaviour
{
	public float delayCena;
	public Vector3 posicao1;
	public Vector3 posicao2;

	private ControladorCena controladorCena;

	void Start ()
	{
		controladorCena = ControladorCena.Pegar();
		StartCoroutine(TrocarCena());
		MoverPara(posicao1);
	}

	void Update ()
	{
		if (controladorCena.state.Buttons.Start == ButtonState.Pressed)
			controladorCena.CarregarCena("intro2");

		if (transform.position == posicao1)
			MoverPara(posicao2);
		else if (transform.position == posicao2)
			MoverPara(posicao1);
	}

	void MoverPara(Vector3 posicao)
	{
		float duracaoMovimento = Vector3.Distance(transform.position, posicao);
		iTween.MoveTo(gameObject, iTween.Hash("position", posicao, "easeType", iTween.EaseType.linear, "time", duracaoMovimento));
	}

	IEnumerator TrocarCena()
	{
		yield return new WaitForSeconds(delayCena);
		controladorCena.CarregarCena("intro2");
	}
}
