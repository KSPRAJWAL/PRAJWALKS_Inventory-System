using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class HandPresence : MonoBehaviour
{
	public InputDeviceCharacteristics controllerCharacteristics;
	private InputDevice targetDevice;
	public Animator handAnimator;

	// Custom grid and trigger value
	GameObject selectedObj;
	bool isCustomGrip = false;
	float customGrip = 1f;

	UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor parentDI;

	void Start()
	{
		TryInitialize();

		// Add listener when object gets selected
		parentDI = transform.parent.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();

		parentDI.selectEntered.AddListener(GetGripValue);
		parentDI.selectExited.AddListener(ResetGripValue);
	}

	void TryInitialize()
	{
		List<InputDevice> devices = new List<InputDevice>();

		InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);
		if (devices.Count > 0)
		{
			targetDevice = devices[0];
		}
	}

	void UpdateHandAnimation()
	{
		if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
		{
			handAnimator.SetFloat("Trigger", triggerValue);
		}
		else
		{
			handAnimator.SetFloat("Trigger", 0);
		}

		if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
		{
			if(isCustomGrip) { handAnimator.SetFloat("Grip", customGrip); }
			else { handAnimator.SetFloat("Grip", gripValue); }
		}
		else
		{
			handAnimator.SetFloat("Grip", 0);
		}
	}

	// This function extracts the custom grab animation parameter value from the tool
	void GetGripValue(SelectEnterEventArgs selectEvent)
	{
		selectedObj = selectEvent.interactableObject.transform.gameObject;

		GrabConfig grabConfig;
		bool hasGrabConfig = selectedObj.TryGetComponent<GrabConfig>(out grabConfig);

		if (selectedObj.layer == 6 && hasGrabConfig)
		{
			customGrip = grabConfig.gripValue;
			isCustomGrip = true;
		}
		else
		{
			isCustomGrip = false;
		}
	}

	// Resets the grip value so that animator is set to default values
	void ResetGripValue(SelectExitEventArgs selectEvent)
	{
		isCustomGrip = false;
	}

	// Update is called once per frame
	void Update()
	{
		if (!targetDevice.isValid)
		{
			TryInitialize();
		}
		else
		{
			UpdateHandAnimation();
		}
	}
}
