using UnityEngine;
using System.Collections;

public class OrdenarCenario : MonoBehaviour
{
	public bool npc;
	public bool parent;
	private SpriteRenderer spriteRenderer;
	private SpriteRenderer spriteRendererModificar;
	internal int orderInicial;

	void Start()
	{
		spriteRendererModificar = GetComponent<SpriteRenderer>();
		orderInicial = spriteRendererModificar.sortingOrder;
		if (npc)
			spriteRenderer = transform.parent.FindChild("NPC").GetComponent<SpriteRenderer>();
		else if (parent)
			spriteRenderer = transform.parent.GetComponent<SpriteRenderer>();
		else
			spriteRenderer = spriteRendererModificar;
	}

	void LateUpdate()
	{
		spriteRendererModificar.sortingOrder = (int)Camera.main.WorldToScreenPoint(spriteRenderer.bounds.min).y * -1 + orderInicial;
	}
}
