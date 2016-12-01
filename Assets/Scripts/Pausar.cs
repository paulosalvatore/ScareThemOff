using UnityEngine;
using UnityEngine.SceneManagement;
using XInputDotNetPure;

public class Pausar : MonoBehaviour
{
	[Header("Sons")]
	public AudioClip somPausar;
	public AudioClip somDespausar;

	private bool startLiberado;
	private bool startPressionado;

	private ControladorCena controladorCena;
	private AudioSource audioSource;

	void Start()
	{
		controladorCena = ControladorCena.Pegar();
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
	}

	void Update()
	{
		if (controladorCena.jogoPausado)
			GamePad.SetVibration(controladorCena.playerIndex, 0f, 0f);

		if (controladorCena.cenaAtiva == 0 || controladorCena.cenaAtiva == 3 || controladorCena.cenaAtiva == 4)
			return;

		if (startLiberado && controladorCena.state.Buttons.Start == ButtonState.Pressed)
		{
			startPressionado = true;
			startLiberado = false;
		}
		else if (!startLiberado && controladorCena.state.Buttons.Start == ButtonState.Released)
			startLiberado = true;

		if ((startPressionado || Input.GetButtonDown("Pausar")) && !controladorCena.jogoPausado)
			PausarJogo();
		else if((startPressionado || Input.GetButtonDown("Pausar")) && controladorCena.jogoPausado)
			DespausarJogo();

		startPressionado = false;
	}

	public void PausarJogo()
	{
		controladorCena.jogoPausado = true;
		Time.timeScale = 0;
		AlterarAudios();
		audioSource.clip = somPausar;
		audioSource.Play();
	}

	public void DespausarJogo()
	{
		controladorCena.jogoPausado = false;
		Time.timeScale = 1;
		AlterarAudios();
		audioSource.clip = somDespausar;
		audioSource.Play();
	}
	
	void AlterarAudios()
	{
		AudioSource[] allAudioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
		foreach (AudioSource audioS in allAudioSources)
		{
			if (controladorCena.jogoPausado)
			{
				if (audioS.isPlaying)
				{
					audioS.maxDistance = audioS.volume;
					audioS.volume = 0f;
					audioS.Pause();
				}
			}
			else
			{
				if (audioS.volume == 0f && audioS.maxDistance > 0f)
				{
					audioS.volume = audioS.maxDistance;
					audioS.maxDistance = 0f;
					audioS.Play();
				}
			}
		}
	}
}
