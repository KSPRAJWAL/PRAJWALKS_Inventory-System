using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DoAfterEnable : MonoBehaviour
{
    public float delay = 0f;
	public bool disableAfterPerforming = true;
	public UnityEvent onPerform;

	bool isCountingDown = false;

	private void OnEnable()
	{
		isCountingDown = false;
		StartCoroutine(_DoAfter(delay));
	}

	IEnumerator _DoAfter(float duration)
	{
		isCountingDown = true;
		yield return new WaitForSeconds(duration);
		
		onPerform.Invoke();
		if(disableAfterPerforming)
			this.gameObject.SetActive(false);
	}

	private void OnDisable()
	{
		if (isCountingDown)
		{
			StopCoroutine(_DoAfter(delay));
			isCountingDown = false;
		}
	}
}
