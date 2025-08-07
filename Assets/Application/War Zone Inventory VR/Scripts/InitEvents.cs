using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InitEvents : MonoBehaviour
{
    public UnityEvent init;

	private void OnEnable()
	{
		init.Invoke();
	}
}
