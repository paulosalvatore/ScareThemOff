using UnityEngine;
using System.Collections;

public class GameOver : MonoBehaviour
{
	ControladorCena controladorCena;

	void Start ()
	{
		controladorCena = ControladorCena.Pegar();
	}

	void Update ()
	{
		if (Input.anyKeyDown)
			controladorCena.CarregarCena("início");
	}
}
